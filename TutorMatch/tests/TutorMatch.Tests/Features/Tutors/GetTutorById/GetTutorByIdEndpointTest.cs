using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Tutors.CreateTutor;
using TutorMatch.Api.Features.Tutors.GetTutorById;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Tutors.GetTutorById;

public class GetTutorByIdEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public GetTutorByIdEndpointTests()
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
    public async Task Given_ExistingTutorId_When_GettingTutorById_Then_ReturnsOkAndTutorDetails()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var availabilityId = Guid.NewGuid();
        var testEmail = "valy_getbyid@tutor.com";
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            
            // Seed Subject
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Subjects (Id, Name) VALUES ({0}, {1})", 
                subjectId, "Physics");

            // Seed Tutor
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy Physics Tutor", testEmail, "Physics Expert", 160);

            // Seed TutorSubjects join table
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO TutorSubjects (TutorId, SubjectId) VALUES ({0}, {1})", 
                tutorId, subjectId);

            // Seed TutorAvailabilities
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO TutorAvailabilities (Id, TutorId, Start, End) VALUES ({0}, {1}, {2}, {3})", 
                availabilityId, tutorId, startTime, endTime);
        }

        // Act
        var response = await _client.GetAsync($"/api/tutors/{tutorId}");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<GetTutorByIdResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(tutorId);
        result.Name.Should().Be("Valy Physics Tutor");
        result.Email.Should().Be(testEmail);
        result.HourlyRate.Should().Be(160);
        
        // Verificăm listele
        result.SubjectIds.Should().Contain(subjectId);
        result.Availability.Should().NotBeEmpty();

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Tutors.AnyAsync(t => t.Id == tutorId);
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Given_NonExistentTutorId_When_GettingTutorById_Then_ReturnsNotFoundWithError()
    {
        // Arrange
        var nonExistentTutorId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/tutors/{nonExistentTutorId}");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Assert - Body (Citire brută a erorii conform regulilor)
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Tutor.NotFound");

        // Assert - Database (Verificăm strict entitatea cerută)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Tutors.AnyAsync(t => t.Id == nonExistentTutorId);
            
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