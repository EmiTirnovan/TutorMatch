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
using TutorMatch.Api.Features.Sessions.GetMySessions;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Sessions.GetMySessions;

public class GetMySessionsEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public GetMySessionsEndpointTests()
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
    public async Task Given_ValidStudentWithSessions_When_GettingMySessions_Then_ReturnsOkAndFilteredSessions()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var start1 = DateTime.UtcNow.AddDays(1);
        var start2 = DateTime.UtcNow.AddDays(2);
        var startOther = DateTime.UtcNow.AddDays(3);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor My1", "tutor_my1@test.com", "Bio", 50, tutorId);
            var student1 = new Student(studentId, "Student My1", "student_my1@test.com");
            var student2 = new Student(otherStudentId, "Student My2", "student_my2@test.com");
            var s1 = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(start1, start1.AddHours(1)));
            var s2 = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(start2, start2.AddHours(1)));
            var sOther = new Session(Guid.NewGuid(), tutorId, otherStudentId, new DateRange(startOther, startOther.AddHours(1)));
            db.Tutors.Add(tutor);
            db.Students.AddRange(student1, student2);
            db.Sessions.AddRange(s1, s2, sOther);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/sessions/my?userId={studentId}&role=Student");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetMySessionsResponse>();
        result.Should().NotBeNull();
        result!.Sessions.Should().HaveCount(2);
        result.Sessions.All(s => s.StudentId == studentId).Should().BeTrue();
    }

    [Fact]
    public async Task Given_StatusFilterUpcoming_When_GettingMySessions_Then_ReturnsOnlyBookedSessions()
    {
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var startBooked = DateTime.UtcNow.AddDays(1);
        var startCancelled = DateTime.UtcNow.AddDays(2);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutor = new Tutor("Tutor My2", "tutor_my2@test.com", "Bio", 50, tutorId);
            var student = new Student(studentId, "Student My2", "student_my2@test.com");
            var sBooked = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(startBooked, startBooked.AddHours(1)));
            var sCancelled = new Session(Guid.NewGuid(), tutorId, studentId, new DateRange(startCancelled, startCancelled.AddHours(1)));
            sCancelled.Cancel();
            db.Tutors.Add(tutor);
            db.Students.Add(student);
            db.Sessions.AddRange(sBooked, sCancelled);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/sessions/my?userId={studentId}&role=Student&status=Upcoming");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetMySessionsResponse>();
        result.Should().NotBeNull();
        result!.Sessions.Should().HaveCount(1);
        result.Sessions[0].Status.Should().Be(SessionStatus.Booked);
    }

    [Fact]
    public async Task Given_InvalidRole_When_GettingMySessions_Then_ReturnsBadRequest()
    {
        var response = await _client.GetAsync($"/api/sessions/my?userId={Guid.NewGuid()}&role=InvalidRole");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Session.Validation");
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}

