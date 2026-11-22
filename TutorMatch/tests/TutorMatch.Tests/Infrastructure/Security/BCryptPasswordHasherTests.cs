using FluentAssertions;
using TutorMatch.Api.Infrastructure.Security;
using Xunit;

namespace TutorMatch.Tests.Infrastructure.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Given_Password_When_Hashing_Then_ReturnsValidBCryptHashAndNotPlainText()
    {
        var password = "SecurePassword123!";

        var hash = _hasher.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
        hash.Should().StartWith("$2"); // BCrypt salt prefix
    }

    [Fact]
    public void Given_ValidPasswordAndHash_When_Verifying_Then_ReturnsTrue()
    {
        var password = "SuperSecretPassword!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword(password, hash);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Given_WrongPasswordAndHash_When_Verifying_Then_ReturnsFalse()
    {
        var password = "SuperSecretPassword!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword("WrongPassword123!", hash);

        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("password", "")]
    [InlineData("", "hash")]
    [InlineData("password", "invalid-hash-string")]
    public void Given_InvalidInputs_When_Verifying_Then_ReturnsFalse(string password, string hash)
    {
        var isValid = _hasher.VerifyPassword(password, hash);

        isValid.Should().BeFalse();
    }
}
