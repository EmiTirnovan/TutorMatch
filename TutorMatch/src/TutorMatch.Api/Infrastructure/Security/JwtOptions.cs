namespace TutorMatch.Api.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TutorMatch";
    public string Audience { get; set; } = "TutorMatchClient";
    public string SecretKey { get; set; } = "SuperSecretKeyForTutorMatchDevelopment2026!MustBeAtLeast32BytesLong";
    public int ExpirationMinutes { get; set; } = 1440; // 24 hours
}
