using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace TutorMatch.Api.Infrastructure.Security;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleAuthOptions _options;

    public GoogleAuthService(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GoogleUserInfo> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idToken, nameof(idToken));

        var settings = new GoogleJsonWebSignature.ValidationSettings();
        if (!string.IsNullOrWhiteSpace(_options.ClientId))
        {
            settings.Audience = new[] { _options.ClientId };
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name);
    }
}
