using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Common.Dtos;
using TutorMatch.Api.Features.Tutors.UpdateAvailability;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Tutors.UpdateAvailability;

public class UpdateAvailabilityEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public UpdateAvailabilityEndpointTests()
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
    public async Task Given_ValidRequestAndExistingTutor_When_UpdatingAvailability_Then_ReturnsNoContentAndSavesToDb()
    {
        var tutorId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})",
                tutorId, "Test Tutor", "test_avail@tutor.com", "Bio", 100);
        }

        var start1 = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var end1 = start1.AddHours(2);
        var start2 = DateTime.UtcNow.Date.AddDays(1).AddHours(14);
        var end2 = start2.AddHours(2);

        var request = new UpdateAvailabilityRequest(new List<DateRangeDto>
        {
            new(start1, end1),
            new(start2, end2)
        });

        var response = await _client.PutAsJsonAsync($"/api/tutors/{tutorId}/availability", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutorInDb = await db.Tutors
                .Include(t => t.Availability)
                .FirstOrDefaultAsync(t => t.Id == tutorId);

            tutorInDb.Should().NotBeNull();
            tutorInDb!.Availability.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task Given_OverlappingAvailability_When_UpdatingAvailability_Then_ReturnsBadRequestAndDoesNotUpdateDb()
    {
        var tutorId = Guid.NewGuid();
        var initialStart = DateTime.UtcNow.Date.AddDays(1).AddHours(8);
        var initialEnd = initialStart.AddHours(1);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})",
                tutorId, "Test Tutor", "test_overlap@tutor.com", "Bio", 100);
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO TutorAvailabilities (Id, Start, End, TutorId) VALUES ({0}, {1}, {2}, {3})",
                Guid.NewGuid(), initialStart, initialEnd, tutorId);
        }

        var overlapStart1 = DateTime.UtcNow.Date.AddDays(2).AddHours(10);
        var overlapEnd1 = overlapStart1.AddHours(3);
        var overlapStart2 = overlapStart1.AddHours(1);
        var overlapEnd2 = overlapStart2.AddHours(3);

        var request = new UpdateAvailabilityRequest(new List<DateRangeDto>
        {
            new(overlapStart1, overlapEnd1),
            new(overlapStart2, overlapEnd2)
        });

        var response = await _client.PutAsJsonAsync($"/api/tutors/{tutorId}/availability", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Tutor.Validation");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutorInDb = await db.Tutors
                .Include(t => t.Availability)
                .FirstOrDefaultAsync(t => t.Id == tutorId);

            tutorInDb.Should().NotBeNull();
            tutorInDb!.Availability.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task Given_NonExistingTutorId_When_UpdatingAvailability_Then_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateAvailabilityRequest(new List<DateRangeDto>());

        var response = await _client.PutAsJsonAsync($"/api/tutors/{nonExistentId}/availability", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Tutor.NotFound");
    }

    [Fact]
    public async Task Given_InvalidDateRange_When_UpdatingAvailability_Then_ReturnsBadRequest()
    {
        var tutorId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})",
                tutorId, "Test Tutor", "test_invalidrange@tutor.com", "Bio", 100);
        }

        var invalidStart = DateTime.UtcNow.Date.AddDays(1).AddHours(12);
        var invalidEnd = DateTime.UtcNow.Date.AddDays(1).AddHours(10);

        var request = new UpdateAvailabilityRequest(new List<DateRangeDto>
        {
            new(invalidStart, invalidEnd)
        });

        var response = await _client.PutAsJsonAsync($"/api/tutors/{tutorId}/availability", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Tutor.Validation");
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}
