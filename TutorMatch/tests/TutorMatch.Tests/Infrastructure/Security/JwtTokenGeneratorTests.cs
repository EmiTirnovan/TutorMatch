using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Infrastructure.Security;
using Xunit;

namespace TutorMatch.Tests.Infrastructure.Security;

public class JwtTokenGeneratorTests
{
    private readonly JwtOptions _options = new()
    {
        Issuer = "TutorMatchTestIssuer",
        Audience = "TutorMatchTestAudience",
        SecretKey = "SuperSecretTestKeyThatIsVeryLongAndSecure1234567890!",
        ExpirationMinutes = 60
    };

    [Fact]
    public void Given_User_When_GeneratingToken_Then_ContainsRequiredClaims()
    {
        // Arrange
        var generator = new JwtTokenGenerator(Options.Create(_options));
        var user = new User(
            Guid.NewGuid(),
            "alex.test@example.com",
            "hashedPass",
            "Alex Test",
            "Student");

        // Act
        var token = generator.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be(_options.Issuer);
        jwt.Audiences.Should().Contain(_options.Audience);

        // Verify required claims according to Definition of Done
        var nameidClaim = jwt.Claims.FirstOrDefault(c => c.Type == "nameid");
        nameidClaim.Should().NotBeNull();
        nameidClaim!.Value.Should().Be(user.Id.ToString());

        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == "email");
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(user.Email);

        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(user.Role);

        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "name");
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be(user.FullName);
    }

    [Fact]
    public void Given_GeneratedToken_When_ValidatingSignatureAndLifetime_Then_TokenIsValid()
    {
        // Arrange
        var generator = new JwtTokenGenerator(Options.Create(_options));
        var user = new User(Guid.NewGuid(), "test@domain.com", "hash", "Full Name", "Tutor");
        var token = generator.GenerateToken(user);

        var tokenValidationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();

        // Act
        var principal = handler.ValidateToken(token, tokenValidationParams, out var validatedToken);

        // Assert
        principal.Should().NotBeNull();
        validatedToken.Should().NotBeNull();
        principal.FindFirst("nameid")?.Value.Should().Be(user.Id.ToString());
        principal.FindFirst("email")?.Value.Should().Be(user.Email);
    }
}
