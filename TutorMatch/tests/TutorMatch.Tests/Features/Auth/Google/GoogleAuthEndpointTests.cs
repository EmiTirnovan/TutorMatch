using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMatch.Api.Features.Auth;
using TutorMatch.Api.Features.Auth.Google;
using TutorMatch.Api.Infrastructure.Persistence;
using TutorMatch.Api.Infrastructure.Security;
using TutorMatch.Tests.Common.Fakes;
using Xunit;

namespace TutorMatch.Tests.Features.Auth.Google;

public class GoogleAuthEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly DbConnection _connection;
    private readonly FakeGoogleAuthService _fakeGoogleAuthService = new();

    public GoogleAuthEndpointTests()
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

                // Replace GoogleAuthService with fake for integration testing
                var googleDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGoogleAuthService));
                if (googleDescriptor != null) services.Remove(googleDescriptor);
                services.AddSingleton<IGoogleAuthService>(_fakeGoogleAuthService);
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        db.Database.EnsureCreated();

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Given_ValidGoogleToken_When_Authenticating_Then_Returns200OkWithJwtToken()
    {
        // Arrange
        _fakeGoogleAuthService.Validator = token => new GoogleUserInfo("sub-google-test-1", "google.endpoint@tutormatch.ro", "Google Endpoint User");
        var request = new GoogleAuthRequest(IdToken: "valid-google-token-xyz");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be("google.endpoint@tutormatch.ro");
        result.FullName.Should().Be("Google Endpoint User");
        result.Token.Should().NotBeNullOrWhiteSpace();

        // Verify user was persisted
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorMatchDbContext>();
        var userInDb = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == "sub-google-test-1");
        userInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_InvalidGoogleToken_When_Authenticating_Then_Returns400BadRequest()
    {
        // Arrange
        _fakeGoogleAuthService.FailWith = new InvalidOperationException("Google token invalid");
        var request = new GoogleAuthRequest(IdToken: "bad-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/google", request);

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
