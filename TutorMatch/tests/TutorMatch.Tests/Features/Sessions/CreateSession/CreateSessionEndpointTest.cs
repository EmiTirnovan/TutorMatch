using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Features.Sessions.CreateSession;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Sessions.CreateSession;

public class CreateSessionEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public CreateSessionEndpointTests()
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
    public async Task Given_ValidRequest_When_CreatingSession_Then_ReturnsCreatedAndSavesToDb()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);
        
        // Seed the database cu Tutor și Student pentru Foreign Keys
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy Tutor", "valy_session@tutor.com", "Bio", 100);
                
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Students (Id, Name, Email) VALUES ({0}, {1}, {2})", 
                studentId, "Student X", "student_x@test.com");
        }

        var request = new CreateSessionRequest(tutorId, studentId, startTime, endTime);

        // Act
        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
        result.Should().NotBeNull();
        result!.TutorId.Should().Be(tutorId);
        result.StudentId.Should().Be(studentId);
        // Regula SKILL_6: Fără ghicit de valori pentru Enum (ex. SessionStatus), deci omitem aserțiunea pe Status

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var sessionInDb = await db.Sessions.FirstOrDefaultAsync(s => s.Id == result.Id);
            
            sessionInDb.Should().NotBeNull();
            sessionInDb!.TutorId.Should().Be(tutorId);
            sessionInDb.StudentId.Should().Be(studentId);
        }
    }

    [Fact]
    public async Task Given_EmptyGuidForTutorId_When_CreatingSession_Then_ReturnsBadRequestWithErrorDetails()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(1);

        // Setăm TutorId ca Guid.Empty pentru a pica prima validare din Handler
        var request = new CreateSessionRequest(Guid.Empty, studentId, startTime, endTime);

        // Act
        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Assert - Body (Citire text brut, verificăm structura de eroare așteptată)
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Session.Validation");

        // Assert - Database (Ne asigurăm cu AnyAsync că nu există sesiunea cerută pentru acest student)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionExists = await db.Sessions.AnyAsync(s => s.StudentId == studentId);
        
        sessionExists.Should().BeFalse();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}