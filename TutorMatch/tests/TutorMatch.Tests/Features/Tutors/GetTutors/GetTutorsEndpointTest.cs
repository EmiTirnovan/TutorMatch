using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Tutors.GetTutors;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Tutors.GetTutors;

public class GetTutorsEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public GetTutorsEndpointTests()
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
    public async Task Given_TutorsExistInDatabase_When_GettingTutors_Then_ReturnsOkWithTutorsList()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var testEmail = "valy_get_test@tutor.com";
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, 
                "Valy Tutor", 
                testEmail, 
                "Great tutor", 
                150);
        }

        // Act
        var response = await _client.GetAsync("/api/tutors");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<List<TutorSummaryResponse>>();
        result.Should().NotBeNull();
        // Verificăm doar dacă lista conține tutorul nostru, pentru a nu pica dacă aplicația are un Global Seed.
        result.Should().Contain(t => t.Email == testEmail);

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Tutors.AnyAsync(t => t.Email == testEmail);
            
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Given_DefaultDatabaseState_When_GettingTutors_Then_ReturnsOkAndNotNullList()
    {
        // Arrange
        // Fără inserări manuale.

        // Act
        var response = await _client.GetAsync("/api/tutors");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<List<TutorSummaryResponse>>();
        // Verificăm doar că nu e null. Nu folosim BeEmpty() pentru că s-ar putea să existe date din seeding.
        result.Should().NotBeNull(); 

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            // Ne asigurăm doar că putem interoga baza de date (chiar dacă rezultatul e o listă goală sau populată global)
            var canQuery = await db.Tutors.AnyAsync();
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}