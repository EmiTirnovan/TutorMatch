using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Sessions.CompleteSession;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.CompleteSession;

public class CompleteSessionEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public CompleteSessionEndpointTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TutorMatchDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

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
    public async Task Given_BookedSessionInPast_When_Completing_Then_ReturnsNoContentAndSetsCompletedInDb()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(-2);
        var end = start.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Complete1", "tutor_comp1@test.com", "Bio", 90, tutorId);
            var student = new Student(studentId, "Student Complete1", "student_comp1@test.com");
            var session = new Session(sessionId, tutorId, studentId, new DateRange(start, end));
            db.Tutors.Add(tutor);
            db.Students.Add(student);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
        }

        var request = new CompleteSessionRequest(tutorId);

        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/complete", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionInDb = await verifyDb.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.Status.Should().Be(SessionStatus.Completed);
        sessionInDb.Version.Should().Be(2);
    }

    [Fact]
    public async Task Given_BookedSessionInFuture_When_Completing_Then_ReturnsConflictAndDoesNotUpdateDb()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Complete2", "tutor_comp2@test.com", "Bio", 90, tutorId);
            var student = new Student(studentId, "Student Complete2", "student_comp2@test.com");
            var session = new Session(sessionId, tutorId, studentId, new DateRange(start, end));
            db.Tutors.Add(tutor);
            db.Students.Add(student);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
        }

        var request = new CompleteSessionRequest(tutorId);

        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/complete", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Conflict");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionInDb = await verifyDb.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.Status.Should().Be(SessionStatus.Booked);
        sessionInDb.Version.Should().Be(1);
    }

    [Fact]
    public async Task Given_DifferentTutor_When_Completing_Then_ReturnsConflict()
    {
        var tutorId = Guid.NewGuid();
        var wrongTutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(-3);
        var end = start.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Complete3", "tutor_comp3@test.com", "Bio", 90, tutorId);
            var student = new Student(studentId, "Student Complete3", "student_comp3@test.com");
            var session = new Session(sessionId, tutorId, studentId, new DateRange(start, end));
            db.Tutors.Add(tutor);
            db.Students.Add(student);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
        }

        var request = new CompleteSessionRequest(wrongTutorId);

        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/complete", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Conflict");
    }

    [Fact]
    public async Task Given_NonExistentSession_When_Completing_Then_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new CompleteSessionRequest(Guid.NewGuid());

        var response = await _client.PostAsJsonAsync($"/api/sessions/{nonExistentId}/complete", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.NotFound");
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}

