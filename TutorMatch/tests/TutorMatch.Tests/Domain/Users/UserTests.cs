using FluentAssertions;
using TutorMatch.Api.Domain.Users;
using Xunit;

namespace TutorMatch.Tests.Domain.Users;

public class UserTests
{
    [Fact]
    public void Given_ValidData_When_CreatingUser_Then_UserIsConstructedCorrectly()
    {
        var id = Guid.NewGuid();
        var email = "TEST.USER@example.com";
        var passwordHash = "$2a$11$somethingHashed";
        var fullName = "Ion Popescu";
        var role = "Student";

        var user = new User(id, email, passwordHash, fullName, role);

        user.Id.Should().Be(id);
        user.Email.Should().Be("test.user@example.com");
        user.PasswordHash.Should().Be(passwordHash);
        user.FullName.Should().Be("Ion Popescu");
        user.Role.Should().Be("Student");
        user.GoogleId.Should().BeNull();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Given_EmptyId_When_CreatingUser_Then_ThrowsArgumentException()
    {
        var act = () => new User(Guid.Empty, "user@test.com", "hash", "Name", "Student");
        act.Should().Throw<ArgumentException>().WithMessage("*ID cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    public void Given_InvalidEmail_When_CreatingUser_Then_ThrowsArgumentException(string email)
    {
        var act = () => new User(Guid.NewGuid(), email, "hash", "Name", "Student");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_BlankFullName_When_CreatingUser_Then_ThrowsArgumentException(string name)
    {
        var act = () => new User(Guid.NewGuid(), "user@test.com", "hash", name, "Student");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Given_GoogleId_When_SettingGoogleId_Then_UpdatesGoogleId()
    {
        var user = new User(Guid.NewGuid(), "user@test.com", null, "Name", "Student");
        user.SetGoogleId("google-sub-456");

        user.GoogleId.Should().Be("google-sub-456");
    }

    [Fact]
    public void Given_NewPasswordHash_When_SettingPasswordHash_Then_UpdatesPasswordHash()
    {
        var user = new User(Guid.NewGuid(), "user@test.com", null, "Name", "Student");
        user.SetPasswordHash("$2a$11$newhash");

        user.PasswordHash.Should().Be("$2a$11$newhash");
    }

    [Fact]
    public void Given_NewFullName_When_UpdatingProfile_Then_UpdatesFullName()
    {
        var user = new User(Guid.NewGuid(), "user@test.com", "hash", "Old Name", "Student");
        user.UpdateProfile("New Name");

        user.FullName.Should().Be("New Name");
    }
}
