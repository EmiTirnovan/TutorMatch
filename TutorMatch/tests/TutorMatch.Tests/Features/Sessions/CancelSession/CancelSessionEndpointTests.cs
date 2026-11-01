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
using TutorMatch.Api.Features.Sessions.CancelSession;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.CancelSession;

public class CancelSessionEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public CancelSessionEndpointTests()
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
    public async Task Given_BookedSessionMoreThan12HoursBeforeStart_When_Cancelling_Then_ReturnsNoContentAndSetsCancelledInDb()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var start = DateTime.UtcNow.Date.AddDays(3).AddHours(14);
        var end = start.AddHours(2);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Cancel1", "tutor_c1@test.com", "Bio", 60, tutorId);
            var student = new Student(studentId, "Student Cancel1", "student_c1@test.com");
            var session = new Session(sessionId, tutorId, studentId, new DateRange(start, end));
            db.Tutors.Add(tutor);
            db.Students.Add(student);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
        }

        var request = new CancelSessionRequest(studentId);

        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/cancel", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionInDb = await verifyDb.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.Status.Should().Be(SessionStatus.Cancelled);
        sessionInDb.Version.Should().Be(2);
    }

    [Fact]
    public async Task Given_BookedSessionLessThan12HoursBeforeStart_When_Cancelling_Then_ReturnsNoContentAndSetsLateCancelledInDb()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(2);
        var end = start.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Cancel2", "tutor_c2@test.com", "Bio", 70, tutorId);
            var student = new Student(studentId, "Student Cancel2", "student_c2@test.com");
            var session = new Session(sessionId, tutorId, studentId, new DateRange(start, end));
            db.Tutors.Add(tutor);
            db.Students.Add(student);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
        }

        var request = new CancelSessionRequest(tutorId);

        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/cancel", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionInDb = await verifyDb.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.Status.Should().Be(SessionStatus.LateCancelled);
        sessionInDb.Version.Should().Be(2);
    }

    [Fact]
    public async Task Given_UnauthorizedRequester_When_Cancelling_Then_ReturnsConflictAndDoesNotChangeDb()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var start = DateTime.UtcNow.Date.AddDays(2).AddHours(10);
        var end = start.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Cancel3", "tutor_c3@test.com", "Bio", 80, tutorId);
            var student = new Student(studentId, "Student Cancel3", "student_c3@test.com");
            var session = new Session(sessionId, tutorId, studentId, new DateRange(start, end));
            db.Tutors.Add(tutor);
            db.Students.Add(student);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
        }

        var request = new CancelSessionRequest(strangerId);

        var response = await _client.PostAsJsonAsync($"/api/sessions/{sessionId}/cancel", request);

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
    public async Task Given_NonExistentSession_When_Cancelling_Then_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new CancelSessionRequest(Guid.NewGuid());

        var response = await _client.PostAsJsonAsync($"/api/sessions/{nonExistentId}/cancel", request);

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

