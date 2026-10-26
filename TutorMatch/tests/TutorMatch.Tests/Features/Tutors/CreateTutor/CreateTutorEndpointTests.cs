using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Tutors.CreateTutor;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Tests.Features.Tutors.CreateTutor;

public class CreateTutorEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public CreateTutorEndpointTests()
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
    public async Task Given_ValidRequest_When_CreatingTutor_Then_ReturnsCreatedAndSavesToDb()
    {
        // Arrange
        var subjectId = Guid.NewGuid();
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Subjects (Id, Name) VALUES ({0}, {1})", 
                subjectId, 
                "Mathematics");
        }

        var request = new CreateTutorRequest(
            "Valy",
            "valy@test.com",
            "Expert Math Tutor",
            150,
            new List<Guid> { subjectId },
            new List<DateRangeDto> { new DateRangeDto(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2)) }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/tutors", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Assert - Body
        var result = await response.Content.ReadFromJsonAsync<CreateTutorResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Valy");
        result.Email.Should().Be("valy@test.com");
        result.HourlyRate.Should().Be(150);

        // Assert - Database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
            var tutorInDb = await db.Tutors.FirstOrDefaultAsync(t => t.Id == result.Id);
            
            tutorInDb.Should().NotBeNull();
            tutorInDb!.Email.Should().Be("valy@test.com");
        }
    }

    [Fact]
    public async Task Given_InvalidRequest_When_CreatingTutor_Then_ReturnsBadRequestWithErrorDetails()
    {
        // Arrange
        var request = new CreateTutorRequest(
            "", 
            "invalid-email@test.com",
            "",
            -10, 
            null,
            null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/tutors", request);

        // Assert - HTTP
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Assert - Body 
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("Tutor.Validation");

        // Assert - Database (Verificare strictă de izolare)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var exists = await db.Tutors.AnyAsync(t => t.Email == request.Email);
        
        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}