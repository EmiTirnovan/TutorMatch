using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Auth;
using TutorMatch.Api.Features.Auth.Register;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Auth.Register;

public class RegisterEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;

    public RegisterEndpointTests()
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
    public async Task Given_ValidRegistrationRequest_When_Registering_Then_Returns201CreatedAndSavesHashedPassword()
    {
        // Arrange
        var request = new RegisterRequest(
            "student.test@tutormatch.ro",
            "StrongPassword123!",
            "Elena Dobre",
            "Student");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("student.test@tutormatch.ro");
        result.FullName.Should().Be("Elena Dobre");
        result.Role.Should().Be("Student");
        result.Token.Should().NotBeNullOrWhiteSpace();

        // Assert Database: No plain text password!
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var userInDb = await db.Users.FirstOrDefaultAsync(u => u.Email == "student.test@tutormatch.ro");

        userInDb.Should().NotBeNull();
        userInDb!.PasswordHash.Should().NotBe("StrongPassword123!");
        userInDb.PasswordHash.Should().StartWith("$2");
    }

    [Fact]
    public async Task Given_DuplicateEmail_When_Registering_Then_Returns409Conflict()
    {
        // Arrange: First registration
        var request = new RegisterRequest("duplicate@tutormatch.ro", "Password123!", "First User", "Student");
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: Second registration with same email
        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Given_InvalidRequest_When_Registering_Then_Returns400BadRequest()
    {
        // Arrange: Invalid email and short password
        var request = new RegisterRequest("not-an-email", "123", "", "Student");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _connection.Dispose();
        _factory.Dispose();
        _client.Dispose();
    }
}
