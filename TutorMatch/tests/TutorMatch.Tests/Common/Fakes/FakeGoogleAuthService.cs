using TutorMatch.Api.Infrastructure.Security;

namespace TutorMatch.Tests.Common.Fakes;

public class FakeGoogleAuthService : IGoogleAuthService
{
    public Func<string, GoogleUserInfo>? Validator { get; set; }
    public Exception? FailWith { get; set; }

    public Task<GoogleUserInfo> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        if (Validator is not null)
        {
            return Task.FromResult(Validator(idToken));
        }

        return Task.FromResult(new GoogleUserInfo("google-user-123", "google.user@example.com", "Google User"));
    }
}
