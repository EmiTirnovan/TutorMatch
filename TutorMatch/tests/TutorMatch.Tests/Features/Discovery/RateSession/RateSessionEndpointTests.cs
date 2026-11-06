using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Discovery.RateSession;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Discovery.RateSession;

public class RateSessionEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _connection;
    private readonly HttpClient _client;

    public RateSessionEndpointTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TutorMatchDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<TutorMatchDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        db.Database.EnsureCreated();

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Given_ValidRequest_When_RatingSession_Then_ReturnsSuccessAndSavesRating()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Tutor Rating", "rating@test.com", "Bio", 100);
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Students (Id, Name, Email) VALUES ({0}, {1}, {2})", 
                studentId, "Student Rating", "srating@test.com");
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Sessions (Id, TutorId, StudentId, StartTime, EndTime, Status, Version) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})", 
                sessionId, tutorId, studentId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "Completed", 1);
        }

        var request = new RateSessionRequest(5, "Foarte bun!");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/tutors/{tutorId}/sessions/{sessionId}/rate");
        requestMessage.Headers.Add("X-Student-Id", studentId.ToString());
        requestMessage.Content = JsonContent.Create(request);

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var rating = await db.Ratings.FirstOrDefaultAsync(r => r.SessionId == sessionId);
            
            rating.Should().NotBeNull();
            rating!.Score.Should().Be(5);
        }
    }

    [Fact]
    public async Task Given_BookedSession_When_RatingSession_Then_ReturnsConflictAndSavesNothing()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})",
                tutorId, "Tutor Booked", "booked@test.com", "Bio", 100);
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Students (Id, Name, Email) VALUES ({0}, {1}, {2})",
                studentId, "Student Booked", "sbooked@test.com");
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Sessions (Id, TutorId, StudentId, StartTime, EndTime, Status, Version) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                sessionId, tutorId, studentId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1), "Booked", 1);
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/tutors/{tutorId}/sessions/{sessionId}/rate");
        requestMessage.Headers.Add("X-Student-Id", studentId.ToString());
        requestMessage.Content = JsonContent.Create(new RateSessionRequest(5, "Prea devreme"));

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            (await db.Ratings.AnyAsync(r => r.SessionId == sessionId)).Should().BeFalse();
        }
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}