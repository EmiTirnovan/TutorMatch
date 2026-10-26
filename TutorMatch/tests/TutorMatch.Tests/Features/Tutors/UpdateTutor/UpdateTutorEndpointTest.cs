using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Tutors.UpdateTutor;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Tutors.UpdateTutor;

public class UpdateTutorEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public UpdateTutorEndpointTests()
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
    public async Task Given_ValidRequestAndExistingTutor_When_UpdatingTutor_Then_ReturnsSuccessAndUpdatesDb()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var originalEmail = "valy_update@tutor.com";
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy Original", originalEmail, "Original Bio", 100);
        }

        var request = new UpdateTutorRequest("Valy Updated", "Updated Bio", 200);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tutors/{tutorId}", request);

        // Assert - HTTP
        // Un update cu succes fără returnare de date se mapează de obicei la 204 NoContent (sau 200 OK în funcție de maparea voastră)
        response.IsSuccessStatusCode.Should().BeTrue();
        
        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var updatedTutor = await db.Tutors.FirstOrDefaultAsync(t => t.Id == tutorId);
            
            updatedTutor.Should().NotBeNull();
            updatedTutor!.Name.Should().Be("Valy Updated");
            updatedTutor.Bio.Should().Be("Updated Bio");
            updatedTutor.HourlyRate.Should().Be(200);
            
            // Verificăm că emailul a rămas neschimbat (nu face parte din request)
            updatedTutor.Email.Should().Be(originalEmail);
        }
    }

    [Fact]
    public async Task Given_NonExistentTutorId_When_UpdatingTutor_Then_ReturnsNotFoundWithError()
    {
        // Arrange
        var nonExistentTutorId = Guid.NewGuid();
        var request = new UpdateTutorRequest("Any Name", "Any Bio", 150);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tutors/{nonExistentTutorId}", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Assert - Body 
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Tutor.NotFound");

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Tutors.AnyAsync(t => t.Id == nonExistentTutorId);
            
            exists.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Given_InvalidRequestData_When_UpdatingTutor_Then_ReturnsBadRequestWithErrorAndDoesNotUpdateDb()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy Valid", "valy_badreq@tutor.com", "Valid Bio", 100);
        }

        // Simulam date invalide care ar arunca ArgumentException in entitatea Tutor
        var request = new UpdateTutorRequest("", "Bad Bio", -50);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tutors/{tutorId}", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Assert - Body
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Tutor.Validation");

        // Assert - Database (Verificam că tutorul NU a fost actualizat)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var unmodifiedTutor = await db.Tutors.FirstOrDefaultAsync(t => t.Id == tutorId);
            
            unmodifiedTutor.Should().NotBeNull();
            unmodifiedTutor!.Name.Should().Be("Valy Valid");
            unmodifiedTutor.Bio.Should().Be("Valid Bio");
            unmodifiedTutor.HourlyRate.Should().Be(100);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}