using FluentAssertions;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Features.Auth.Register;
using TutorMatch.Api.Infrastructure.Security;
using TutorMatch.Tests.Common.Fakes;
using Xunit;

namespace TutorMatch.Tests.Features.Auth.Register;

public class RegisterCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeStudentRepository _studentRepository = new();
    private readonly BCryptPasswordHasher _passwordHasher = new();
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32BytesLongForSecurity!",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });
        _jwtTokenGenerator = new JwtTokenGenerator(jwtOptions);
        _handler = new RegisterCommandHandler(_userRepository, _studentRepository, _passwordHasher, _jwtTokenGenerator);
    }

    [Fact]
    public async Task Given_ValidStudentRegistration_When_Handling_Then_CreatesUserAndStudentAndReturnsToken()
    {
        // Arrange
        var command = new RegisterCommand("student@tutormatch.ro", "Password123!", "Ion Popescu", "Student");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be("student@tutormatch.ro");
        result.Value.FullName.Should().Be("Ion Popescu");
        result.Value.Role.Should().Be("Student");
        result.Value.Token.Should().NotBeNullOrWhiteSpace();

        // Verify database state: password must NOT be in plain text
        var userInDb = _userRepository.GetByEmail("student@tutormatch.ro");
        userInDb.Should().NotBeNull();
        userInDb!.PasswordHash.Should().NotBe("Password123!");
        userInDb.PasswordHash.Should().StartWith("$2");
        _passwordHasher.VerifyPassword("Password123!", userInDb.PasswordHash!).Should().BeTrue();

        // Verify Student aggregate created
        var studentInDb = _studentRepository.GetById(userInDb.Id);
        studentInDb.Should().NotBeNull();
        studentInDb!.Name.Should().Be("Ion Popescu");
    }

    [Fact]
    public async Task Given_ValidTutorRegistration_When_Handling_Then_CreatesUserWithTutorRole()
    {
        // Arrange
        var command = new RegisterCommand("tutor@tutormatch.ro", "Password123!", "Maria Ionescu", "Tutor");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Tutor");

        var userInDb = _userRepository.GetByEmail("tutor@tutormatch.ro");
        userInDb.Should().NotBeNull();
        userInDb!.Role.Should().Be("Tutor");
        _studentRepository.GetById(userInDb.Id).Should().BeNull();
    }

    [Fact]
    public async Task Given_ExistingEmail_When_Registering_Then_ReturnsConflict()
    {
        // Arrange
        var existing = new User(Guid.NewGuid(), "existing@tutormatch.ro", "hash", "Existing User", "Student");
        _userRepository.Add(existing);

        var command = new RegisterCommand("existing@tutormatch.ro", "Password123!", "Another Name", "Student");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Auth.EmailAlreadyExists");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task Given_InvalidEmail_When_Registering_Then_ReturnsValidationError(string email)
    {
        var command = new RegisterCommand(email, "Password123!", "Name", "Student");
        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Auth.InvalidEmail");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")] // less than 6 chars
    public async Task Given_ShortOrEmptyPassword_When_Registering_Then_ReturnsValidationError(string password)
    {
        var command = new RegisterCommand("valid@tutormatch.ro", password, "Name", "Student");
        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Auth.InvalidPassword");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Given_EmptyName_When_Registering_Then_ReturnsValidationError(string name)
    {
        var command = new RegisterCommand("valid@tutormatch.ro", "Password123!", name, "Student");
        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Auth.InvalidName");
    }
}
