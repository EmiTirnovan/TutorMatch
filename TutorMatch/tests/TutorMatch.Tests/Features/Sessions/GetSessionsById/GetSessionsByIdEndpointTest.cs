using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Sessions.GetSessionById;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Sessions.GetSessionById;

public class GetSessionByIdEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public GetSessionByIdEndpointTests()
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
    public async Task Given_ExistingSessionId_When_GettingSessionById_Then_ReturnsOkAndSessionDetails()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);
        
        // Seed the database cu Tutor, Student și Session
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy Tutor", "valy_get_session@tutor.com", "Bio", 100);
                
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Students (Id, Name, Email) VALUES ({0}, {1}, {2})", 
                studentId, "Student X", "student_x@test.com");
                
            // Inserăm sesiunea direct, inclusiv câmpurile cerute (Status: presupunem că valoarea numerică 0 e default în SQLite)
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Sessions (Id, TutorId, StudentId, StartTime, EndTime, Status, Version) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})", 
                sessionId, tutorId, studentId, startTime, endTime, 0, 1);
        }

        // Act
        // Presupunem că grupul de rutare are prefixul /api/sessions conform standardului precedent
        var response = await _client.GetAsync($"/api/sessions/{sessionId}");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<GetSessionByIdResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.TutorId.Should().Be(tutorId);
        result.StudentId.Should().Be(studentId);
        // Validăm doar că nu asertăm valori enum necunoscute
        result.Version.Should().Be(1);

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Sessions.AnyAsync(s => s.Id == sessionId);
            
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Given_NonExistentSessionId_When_GettingSessionById_Then_ReturnsNotFoundWithError()
    {
        // Arrange
        var nonExistentSessionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/sessions/{nonExistentSessionId}");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Assert - Body (Citire brută a erorii pentru evitarea deserializării nesigure)
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Session.NotFound");

        // Assert - Database (Izolare: entitatea respectivă nu există)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Sessions.AnyAsync(s => s.Id == nonExistentSessionId);
            
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