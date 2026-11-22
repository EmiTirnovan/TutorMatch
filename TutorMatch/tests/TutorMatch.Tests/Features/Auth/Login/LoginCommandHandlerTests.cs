using FluentAssertions;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Features.Auth.Login;
using TutorMatch.Api.Infrastructure.Security;
using TutorMatch.Tests.Common.Fakes;
using Xunit;

namespace TutorMatch.Tests.Features.Auth.Login;

public class LoginCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly BCryptPasswordHasher _passwordHasher = new();
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32BytesLongForSecurity!",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });
        _jwtTokenGenerator = new JwtTokenGenerator(jwtOptions);
        _handler = new LoginCommandHandler(_userRepository, _passwordHasher, _jwtTokenGenerator);
    }

    [Fact]
    public async Task Given_ValidCredentials_When_LoggingIn_Then_ReturnsAuthResponseWithJwt()
    {
        // Arrange
        var password = "CorrectPassword123!";
        var user = new User(
            Guid.NewGuid(),
            "user@tutormatch.ro",
            _passwordHasher.HashPassword(password),
            "Alex Test",
            "Student");
        _userRepository.Add(user);

        var command = new LoginCommand("user@tutormatch.ro", password);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.FullName.Should().Be(user.FullName);
        result.Value.Role.Should().Be(user.Role);
        result.Value.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Given_WrongPassword_When_LoggingIn_Then_ReturnsValidationError()
    {
        // Arrange
        var user = new User(
            Guid.NewGuid(),
            "user@tutormatch.ro",
            _passwordHasher.HashPassword("CorrectPassword123!"),
            "Alex Test",
            "Student");
        _userRepository.Add(user);

        var command = new LoginCommand("user@tutormatch.ro", "WrongPassword!");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Given_UnknownEmail_When_LoggingIn_Then_ReturnsValidationError()
    {
        var command = new LoginCommand("nonexistent@tutormatch.ro", "Password123!");

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Given_GoogleUserWithoutPassword_When_LoggingInWithPassword_Then_ReturnsFriendlyValidationError()
    {
        // Arrange
        var user = new User(
            Guid.NewGuid(),
            "google.only@tutormatch.ro",
            passwordHash: null,
            "Google User",
            "Student",
            googleId: "google-id-123");
        _userRepository.Add(user);

        var command = new LoginCommand("google.only@tutormatch.ro", "SomePassword!");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Description.Should().Contain("Google");
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("email@test.com", "")]
    public async Task Given_EmptyInputs_When_LoggingIn_Then_ReturnsValidationError(string email, string password)
    {
        var command = new LoginCommand(email, password);
        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
