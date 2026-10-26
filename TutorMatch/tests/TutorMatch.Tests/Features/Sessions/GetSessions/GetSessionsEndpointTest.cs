using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Sessions.GetSessions;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Sessions.GetSessions;

public class GetSessionsEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public GetSessionsEndpointTests()
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
    public async Task Given_SessionsExistInDatabase_When_GettingSessions_Then_ReturnsOkWithSessionsList()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(2);
        var endTime = startTime.AddHours(1);
        
        // Seed the database with Foreign Key dependencies and the Session itself using Raw SQL
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Tutors (Id, Name, Email, Bio, HourlyRate) VALUES ({0}, {1}, {2}, {3}, {4})", 
                tutorId, "Valy Tutor", "valy_getsessions@tutor.com", "Bio", 100);
                
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Students (Id, Name, Email) VALUES ({0}, {1}, {2})", 
                studentId, "Student Y", "student_y@test.com");
                
            // Inserăm sesiunea (Status numeric presupus 0 în DB)
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Sessions (Id, TutorId, StudentId, StartTime, EndTime, Status, Version) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})", 
                sessionId, tutorId, studentId, startTime, endTime, 0, 1);
        }

        // Act
        // Presupunem că prefixul este configurat la /api/sessions conform standardelor anterioare
        var response = await _client.GetAsync("/api/sessions");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<List<GetSessionsResponse>>();
        result.Should().NotBeNull();
        
        // Verificăm doar dacă lista conține sesiunea noastră, ignorând potențiale date de seeding global
        result.Should().Contain(s => s.Id == sessionId && s.TutorId == tutorId && s.StudentId == studentId);

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var exists = await db.Sessions.AnyAsync(s => s.Id == sessionId);
            
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Given_DefaultDatabaseState_When_GettingSessions_Then_ReturnsOkAndNotNullList()
    {
        // Arrange
        // Fără inserări manuale.

        // Act
        var response = await _client.GetAsync("/api/sessions");

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<List<GetSessionsResponse>>();
        result.Should().NotBeNull(); // Nu folosim BeEmpty() conform regulii de Seed Independence.

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var canQuery = await db.Sessions.AnyAsync(); // Ne asigurăm doar că interogarea DB funcționează fără excepții.
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}