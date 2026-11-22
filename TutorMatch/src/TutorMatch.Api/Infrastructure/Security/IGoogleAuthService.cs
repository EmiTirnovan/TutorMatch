namespace TutorMatch.Api.Infrastructure.Security;

public record GoogleUserInfo(string GoogleId, string Email, string Name);

public interface IGoogleAuthService
{
    Task<GoogleUserInfo> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
