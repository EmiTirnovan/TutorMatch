using TutorMatch.Api.Domain.Users;

namespace TutorMatch.Api.Infrastructure.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
