namespace TutorMatch.Api.Infrastructure.Security;

public class GoogleAuthOptions
{
    public const string SectionName = "Google";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
