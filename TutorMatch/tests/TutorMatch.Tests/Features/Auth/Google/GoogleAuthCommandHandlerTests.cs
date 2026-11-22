using FluentAssertions;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Features.Auth.Google;
using TutorMatch.Api.Infrastructure.Security;
using TutorMatch.Tests.Common.Fakes;
using Xunit;

namespace TutorMatch.Tests.Features.Auth.Google;

public class GoogleAuthCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeStudentRepository _studentRepository = new();
    private readonly FakeGoogleAuthService _googleAuthService = new();
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly GoogleAuthCommandHandler _handler;

    public GoogleAuthCommandHandlerTests()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32BytesLongForSecurity!",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });
        _jwtTokenGenerator = new JwtTokenGenerator(jwtOptions);
        _handler = new GoogleAuthCommandHandler(_userRepository, _studentRepository, _googleAuthService, _jwtTokenGenerator);
    }

    [Fact]
    public async Task Given_ValidGoogleToken_When_UserDoesNotExist_Then_CreatesUserAndReturnsToken()
    {
        // Arrange
        _googleAuthService.Validator = token => new GoogleUserInfo("google-123", "new.google@tutormatch.ro", "Google Student");
        var command = new GoogleAuthCommand("valid-token", "Student");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be("new.google@tutormatch.ro");
        result.Value.FullName.Should().Be("Google Student");
        result.Value.Role.Should().Be("Student");
        result.Value.Token.Should().NotBeNullOrWhiteSpace();

        var userInDb = _userRepository.GetByGoogleId("google-123");
        userInDb.Should().NotBeNull();
        userInDb!.PasswordHash.Should().BeNull(); // No plain text password!
        userInDb.GoogleId.Should().Be("google-123");

        var studentInDb = _studentRepository.GetById(userInDb.Id);
        studentInDb.Should().NotBeNull();
        studentInDb!.Name.Should().Be("Google Student");
    }

    [Fact]
    public async Task Given_ValidGoogleToken_When_UserAlreadyExistsByGoogleId_Then_LogsInSuccessfully()
    {
        // Arrange
        var existingUser = new User(
            Guid.NewGuid(),
            "existing.google@tutormatch.ro",
            null,
            "Existing Google User",
            "Student",
            "google-456");
        _userRepository.Add(existingUser);

        _googleAuthService.Validator = token => new GoogleUserInfo("google-456", "existing.google@tutormatch.ro", "Existing Google User");
        var command = new GoogleAuthCommand("valid-token");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(existingUser.Id);
        result.Value.Email.Should().Be(existingUser.Email);
    }

    [Fact]
    public async Task Given_ValidGoogleToken_When_UserExistsByEmailWithoutGoogleId_Then_LinksGoogleAccount()
    {
        // Arrange: User registered originally via email/password
        var existingUser = new User(
            Guid.NewGuid(),
            "email.user@tutormatch.ro",
            "someHash",
            "Email User",
            "Student");
        _userRepository.Add(existingUser);

        _googleAuthService.Validator = token => new GoogleUserInfo("google-789", "email.user@tutormatch.ro", "Email User");
        var command = new GoogleAuthCommand("valid-token");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(existingUser.Id);

        var updatedUser = _userRepository.GetById(existingUser.Id);
        updatedUser!.GoogleId.Should().Be("google-789");
    }

    [Fact]
    public async Task Given_InvalidGoogleToken_When_Validating_Then_ReturnsValidationError()
    {
        // Arrange
        _googleAuthService.FailWith = new InvalidOperationException("Signature verification failed");
        var command = new GoogleAuthCommand("invalid-token");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Auth.GoogleTokenInvalid");
    }

    [Fact]
    public async Task Given_EmptyToken_When_Handling_Then_ReturnsValidationError()
    {
        var command = new GoogleAuthCommand("");

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Auth.InvalidGoogleToken");
    }
}
