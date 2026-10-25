using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Sessions.UpdateSession;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Sessions.UpdateSession;

public class UpdateSessionEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public UpdateSessionEndpointTests()
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
    public async Task Given_ValidRequestAndExistingSession_When_UpdatingSession_Then_ReturnsSuccessAndUpdatesDb()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        
        var originalStartTime = DateTime.UtcNow.AddDays(1);
        var originalEndTime = originalStartTime.AddHours(1);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            
            // Seed Tutor și Student
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy Tutor", "valy_update_session@tutor.com", "Bio", 100);
                
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Students (Id, Name, Email) VALUES ({0}, {1}, {2})", 
                studentId, "Student X", "student_update@test.com");
                
            // Seed Session
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Sessions (Id, TutorId, StudentId, StartTime, EndTime, Status, Version) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})", 
                sessionId, tutorId, studentId, originalStartTime, originalEndTime, 0, 1);
        }

        var newStartTime = DateTime.UtcNow.AddDays(2);
        var newEndTime = newStartTime.AddHours(2);
        var request = new UpdateSessionRequest(newStartTime, newEndTime);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/sessions/{sessionId}", request);

        // Assert - HTTP
        response.IsSuccessStatusCode.Should().BeTrue();
        
        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var updatedSession = await db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            
            updatedSession.Should().NotBeNull();
            // Verificăm dacă datele au fost actualizate corect
            updatedSession!.StartTime.Should().Be(newStartTime);
            updatedSession.EndTime.Should().Be(newEndTime);
        }
    }

    [Fact]
    public async Task Given_NonExistentSessionId_When_UpdatingSession_Then_ReturnsNotFoundWithError()
    {
        // Arrange
        var nonExistentSessionId = Guid.NewGuid();
        var request = new UpdateSessionRequest(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/sessions/{nonExistentSessionId}", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Assert - Body (Citire brută a erorii)
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Session.NotFound");

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Sessions.AnyAsync(s => s.Id == nonExistentSessionId);
            
            exists.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Given_InvalidDateRange_When_UpdatingSession_Then_ReturnsBadRequestWithErrorAndDoesNotUpdateDb()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        
        var originalStartTime = DateTime.UtcNow.AddDays(1);
        var originalEndTime = originalStartTime.AddHours(1);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy Valid", "valy_badreq_sess@tutor.com", "Bio", 100);
                
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Students (Id, Name, Email) VALUES ({0}, {1}, {2})", 
                studentId, "Student X", "student_badreq@test.com");
                
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Sessions (Id, TutorId, StudentId, StartTime, EndTime, Status, Version) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})", 
                sessionId, tutorId, studentId, originalStartTime, originalEndTime, 0, 1);
        }

        // Simulam date invalide: EndTime < StartTime (aruncă ArgumentException în domain)
        var request = new UpdateSessionRequest(originalStartTime, originalStartTime.AddHours(-1));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/sessions/{sessionId}", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Assert - Body
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Session.Validation");

        // Assert - Database (Verificam că sesiunea NU a fost modificată)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var unmodifiedSession = await db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            
            unmodifiedSession.Should().NotBeNull();
            unmodifiedSession!.StartTime.Should().Be(originalStartTime);
            unmodifiedSession.EndTime.Should().Be(originalEndTime);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}