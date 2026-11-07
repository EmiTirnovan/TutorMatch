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
using TutorMatch.Api.Features.Sessions.BookSession;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.BookSession;

public class BookSessionEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public BookSessionEndpointTests()
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
    public async Task Given_ValidRequest_When_BookingSession_Then_ReturnsCreatedAndSavesToDb()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var endTime = startTime.AddHours(2);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Alex", "tutor_alex@test.com", "Bio", 80, tutorId);
            tutor.AddAvailability(new DateRange(startTime.AddHours(-1), endTime.AddHours(1)));
            db.Tutors.Add(tutor);
            var student = new Student(studentId, "Student Maria", "student_maria@test.com");
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        var request = new BookSessionRequest(tutorId, studentId, startTime, endTime);

        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<BookSessionResponse>();
        result.Should().NotBeNull();
        result!.TutorId.Should().Be(tutorId);
        result.StudentId.Should().Be(studentId);
        result.Version.Should().Be(1);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionInDb = await verifyDb.Sessions.FirstOrDefaultAsync(s => s.Id == result.Id);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.TutorId.Should().Be(tutorId);
        sessionInDb.StudentId.Should().Be(studentId);
    }

    [Fact]
    public async Task Given_DuplicateBookingOnSameSlot_When_BookingSecondTime_Then_ReturnsConflict()
    {
        var tutorId = Guid.NewGuid();
        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(14);
        var endTime = startTime.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Dan", "tutor_dan@test.com", "Bio", 90, tutorId);
            tutor.AddAvailability(new DateRange(startTime.AddHours(-1), endTime.AddHours(1)));
            db.Tutors.Add(tutor);
            var student1 = new Student(student1Id, "Student 1", "student1_dan@test.com");
            var student2 = new Student(student2Id, "Student 2", "student2_dan@test.com");
            db.Students.Add(student1);
            db.Students.Add(student2);
            await db.SaveChangesAsync();
        }

        var request1 = new BookSessionRequest(tutorId, student1Id, startTime, endTime);
        var request2 = new BookSessionRequest(tutorId, student2Id, startTime, endTime);

        var response1 = await _client.PostAsJsonAsync("/api/sessions", request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        var response2 = await _client.PostAsJsonAsync("/api/sessions", request2);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response2.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Conflict");
    }

    [Fact]
    public async Task Given_RequestOutsideTutorAvailability_When_BookingSession_Then_ReturnsBadRequest()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var availableStart = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
        var requestedStart = DateTime.UtcNow.Date.AddDays(1).AddHours(15);
        var requestedEnd = requestedStart.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Outside", "tutor_out@test.com", "Bio", 50, tutorId);
            tutor.AddAvailability(new DateRange(availableStart, availableStart.AddHours(2)));
            db.Tutors.Add(tutor);
            var student = new Student(studentId, "Student Out", "student_out@test.com");
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        var request = new BookSessionRequest(tutorId, studentId, requestedStart, requestedEnd);

        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Validation");
    }

    [Fact]
    public async Task Given_NonExistentTutor_When_BookingSession_Then_ReturnsNotFound()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var endTime = startTime.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var student = new Student(studentId, "Student MissingTutor", "student_mt@test.com");
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        var request = new BookSessionRequest(tutorId, studentId, startTime, endTime);

        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Tutor.NotFound");
    }

    [Fact]
    public async Task Given_StudentWithTwoSessionsThatDay_When_BookingThirdSession_Then_ReturnsBadRequestWithDailyLimitCode()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var day = DateTime.UtcNow.Date.AddDays(1);
        var requestedStart = day.AddHours(14);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Limit", "tutor_limit@test.com", "Bio", 70, tutorId);
            tutor.AddAvailability(new DateRange(day.AddHours(8), day.AddHours(18)));
            db.Tutors.Add(tutor);
            db.Students.Add(new Student(studentId, "Student Limit", "student_limit@test.com"));
            db.Sessions.Add(Session.Create(tutorId, studentId, new DateRange(day.AddHours(9), day.AddHours(10))));
            db.Sessions.Add(Session.Create(tutorId, studentId, new DateRange(day.AddHours(11), day.AddHours(12))));
            await db.SaveChangesAsync();
        }

        var request = new BookSessionRequest(tutorId, studentId, requestedStart, requestedStart.AddHours(1));

        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.DailyLimit");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var studentSessionStarts = await verifyDb.Sessions
            .Where(s => s.StudentId == studentId)
            .Select(s => s.Interval.Start)
            .ToListAsync();
        studentSessionStarts.Should().HaveCount(2).And.NotContain(requestedStart);
    }

    [Fact]
    public async Task Given_EmptyTutorId_When_BookingSession_Then_ReturnsBadRequestWithValidationCode()
    {
        var studentId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var request = new BookSessionRequest(Guid.Empty, studentId, startTime, startTime.AddHours(1));

        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Validation");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionSaved = await verifyDb.Sessions.AnyAsync(s => s.StudentId == studentId);
        sessionSaved.Should().BeFalse();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}
