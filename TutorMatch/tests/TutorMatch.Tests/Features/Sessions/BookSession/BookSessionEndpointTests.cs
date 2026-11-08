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
    private static readonly Guid StaticTutorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StaticStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StaticStudent2Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid StaticMissingTutorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

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
        var tutorId = StaticTutorId;
        var studentId = StaticStudentId;
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

        var request = new BookSessionRequest(tutorId, startTime, endTime);

        var response = await BookAsync(studentId, request);

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
    public async Task Given_ValidRequestWithSubjectAndNotes_When_BookingSessionViaV2Endpoint_Then_ReturnsCreatedAndSavesSubjectAndNotes()
    {
        var tutorId = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");
        var studentId = Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444");
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(12);
        var endTime = startTime.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor V2", "tutor_v2@test.com", "Bio", 95, tutorId);
            tutor.AddAvailability(new DateRange(startTime.AddHours(-1), endTime.AddHours(1)));
            db.Tutors.Add(tutor);
            var student = new Student(studentId, "Student V2", "student_v2@test.com");
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        var request = new BookSessionRequest(tutorId, startTime, endTime, "Programare Functionala", "Pregatire laborator Haskell");

        var response = await BookAsync(studentId, request, "/api/v2/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<BookSessionResponse>();
        result.Should().NotBeNull();
        result!.Subject.Should().Be("Programare Functionala");
        result.Notes.Should().Be("Pregatire laborator Haskell");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionInDb = await verifyDb.Sessions.FirstOrDefaultAsync(s => s.Id == result.Id);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.Subject.Should().Be("Programare Functionala");
        sessionInDb.Notes.Should().Be("Pregatire laborator Haskell");
    }

    [Fact]
    public async Task Given_DuplicateBookingOnSameSlot_When_BookingSecondTime_Then_ReturnsConflict()
    {
        var tutorId = Guid.Parse("12345678-aaaa-bbbb-cccc-111111111111");
        var student1Id = Guid.Parse("12345678-aaaa-bbbb-cccc-222222222222");
        var student2Id = Guid.Parse("12345678-aaaa-bbbb-cccc-333333333333");
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

        var request1 = new BookSessionRequest(tutorId, startTime, endTime);
        var request2 = new BookSessionRequest(tutorId, startTime, endTime);

        var response1 = await BookAsync(student1Id, request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        var response2 = await BookAsync(student2Id, request2);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await response2.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Conflict");
    }

    [Fact]
    public async Task Given_RequestOutsideTutorAvailability_When_BookingSession_Then_ReturnsBadRequest()
    {
        var tutorId = Guid.Parse("87654321-aaaa-bbbb-cccc-111111111111");
        var studentId = Guid.Parse("87654321-aaaa-bbbb-cccc-222222222222");
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

        var request = new BookSessionRequest(tutorId, requestedStart, requestedEnd);

        var response = await BookAsync(studentId, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Validation");
    }

    [Fact]
    public async Task Given_NonExistentTutor_When_BookingSession_Then_ReturnsNotFound()
    {
        var tutorId = StaticMissingTutorId;
        var studentId = StaticStudentId;
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var endTime = startTime.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var student = new Student(studentId, "Student MissingTutor", "student_mt@test.com");
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }

        var request = new BookSessionRequest(tutorId, startTime, endTime);

        var response = await BookAsync(studentId, request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Tutor.NotFound");
    }

    [Fact]
    public async Task Given_StudentWithTwoSessionsThatDay_When_BookingThirdSession_Then_ReturnsBadRequestWithDailyLimitCode()
    {
        var tutorId = Guid.Parse("99999999-aaaa-bbbb-cccc-111111111111");
        var studentId = Guid.Parse("99999999-aaaa-bbbb-cccc-222222222222");
        var day = DateTime.UtcNow.Date.AddDays(1);
        var requestedStart = day.AddHours(14);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor Limit", "tutor_limit@test.com", "Bio", 70, tutorId);
            tutor.AddAvailability(new DateRange(day.AddHours(8), day.AddHours(18)));
            db.Tutors.Add(tutor);
            db.Students.Add(new Student(studentId, "Student Limit", "student_limit@test.com"));
            db.Sessions.Add(new Session(Guid.Parse("99999999-aaaa-bbbb-cccc-333333333333"), tutorId, studentId, new DateRange(day.AddHours(9), day.AddHours(10))));
            db.Sessions.Add(new Session(Guid.Parse("99999999-aaaa-bbbb-cccc-444444444444"), tutorId, studentId, new DateRange(day.AddHours(11), day.AddHours(12))));
            await db.SaveChangesAsync();
        }

        var request = new BookSessionRequest(tutorId, requestedStart, requestedStart.AddHours(1));

        var response = await BookAsync(studentId, request);

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
        var studentId = StaticStudentId;
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var request = new BookSessionRequest(Guid.Empty, startTime, startTime.AddHours(1));

        var response = await BookAsync(studentId, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Validation");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var sessionSaved = await verifyDb.Sessions.AnyAsync(s => s.StudentId == studentId);
        sessionSaved.Should().BeFalse();
    }

    [Fact]
    public async Task Given_MissingStudentHeader_When_BookingSession_Then_ReturnsBadRequest()
    {
        var startTime = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var request = new BookSessionRequest(StaticTutorId, startTime, startTime.AddHours(1));

        var response = await _client.PostAsJsonAsync("/api/sessions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private Task<HttpResponseMessage> BookAsync(Guid studentId, BookSessionRequest request, string url = "/api/sessions")
    {
        var message = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(request) };
        message.Headers.Add("X-Student-Id", studentId.ToString());
        return _client.SendAsync(message);
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}
