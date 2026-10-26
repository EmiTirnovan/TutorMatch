using System.Data.Common;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Tutors.DeleteTutor;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Tutors.DeleteTutor;

public class DeleteTutorEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public DeleteTutorEndpointTests()
    {
        // 1. Open a shared connection to keep the in-memory DB alive
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 2. Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TutorMatchDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // 3. Add DbContext using the open connection
                services.AddDbContext<TutorMatchDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
        });

        // 4. Ensure schema is created
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        db.Database.EnsureCreated();

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Given_ExistingTutorId_When_DeletingTutor_Then_ReturnsSuccessAndRemovesFromDb()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy To Delete", "valy_delete@tutor.com", "Bio", 100);
        }

        // Act
        var response = await _client.DeleteAsync($"/api/tutors/{tutorId}");

        // Assert - HTTP
        response.IsSuccessStatusCode.Should().BeTrue();
        
        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Tutors.AnyAsync(t => t.Id == tutorId);
            
            exists.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Given_NonExistentTutorId_When_DeletingTutor_Then_ReturnsNotFoundWithError()
    {
        // Arrange
        var nonExistentTutorId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/tutors/{nonExistentTutorId}");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Assert - Body (Citire brută a erorii)
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Tutor.NotFound");

        // Assert - Database (Verificăm strict entitatea cerută)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Tutors.AnyAsync(t => t.Id == nonExistentTutorId);
            
            exists.Should().BeFalse();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}