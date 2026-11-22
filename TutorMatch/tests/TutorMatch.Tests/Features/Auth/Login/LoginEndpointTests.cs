using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Features.Auth;
using TutorMatch.Api.Features.Auth.Login;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Security;
using Xunit;

namespace TutorMatch.Tests.Features.Auth.Login;

public class LoginEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;
    private readonly User _testUser;

    public LoginEndpointTests()
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

        // Seed a known test user with hashed password
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        _testUser = new User(
            Guid.NewGuid(),
            "login.user@tutormatch.ro",
            passwordHasher.HashPassword("LoginSecret123!"),
            "Login User",
            "Student");
        db.Users.Add(_testUser);
        db.SaveChanges();

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Given_ValidCredentials_When_LoggingIn_Then_Returns200OkWithJwtToken()
    {
        // Arrange
        var request = new LoginRequest("login.user@tutormatch.ro", "LoginSecret123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("login.user@tutormatch.ro");
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.FullName.Should().Be("Login User");
    }

    [Fact]
    public async Task Given_ValidJwtToken_When_AccessingAuthorizedEndpoint_Then_ReturnsClaimsSuccessfully()
    {
        // Arrange: Log in to get token
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("login.user@tutormatch.ro", "LoginSecret123!"));
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.Token);

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(_testUser.Id.ToString());
        body.Should().Contain(_testUser.Email);
        body.Should().Contain("Login User");
        body.Should().Contain("Student");
    }

    [Fact]
    public async Task Given_NoToken_When_AccessingAuthorizedEndpoint_Then_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Given_InvalidToken_When_AccessingAuthorizedEndpoint_Then_Returns401Unauthorized()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        var response = await _client.SendAsync(requestMessage);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Given_WrongPassword_When_LoggingIn_Then_Returns400BadRequest()
    {
        // Arrange
        var request = new LoginRequest("login.user@tutormatch.ro", "WrongPassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Given_UnknownEmail_When_LoggingIn_Then_Returns400BadRequest()
    {
        // Arrange
        var request = new LoginRequest("unknown@tutormatch.ro", "SomePassword123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

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
