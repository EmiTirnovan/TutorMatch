using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Infrastructure.Security;

namespace TutorMatch.Api.Features.Auth.Login;

public class LoginCommandHandler : ICommandHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public Task<Result<AuthResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return Task.FromResult(Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidCredentials", "Invalid email or password.")));
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = _userRepository.GetByEmail(normalizedEmail);
        if (user is null)
        {
            return Task.FromResult(Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidCredentials", "Invalid email or password.")));
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return Task.FromResult(Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidCredentials", "This account was registered with Google Sign-In. Please sign in with Google.")));
        }

        var isValidPassword = _passwordHasher.VerifyPassword(command.Password, user.PasswordHash);
        if (!isValidPassword)
        {
            return Task.FromResult(Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidCredentials", "Invalid email or password.")));
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        var response = new AuthResponse(token, user.Id, user.Email, user.FullName, user.Role);

        return Task.FromResult(Result<AuthResponse>.Success(response));
    }
}
