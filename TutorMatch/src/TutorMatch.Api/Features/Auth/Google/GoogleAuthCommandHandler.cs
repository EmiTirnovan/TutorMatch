using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Infrastructure.Security;

namespace TutorMatch.Api.Features.Auth.Google;

public class GoogleAuthCommandHandler : ICommandHandler<GoogleAuthCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public GoogleAuthCommandHandler(
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IGoogleAuthService googleAuthService,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _googleAuthService = googleAuthService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> HandleAsync(GoogleAuthCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidGoogleToken", "Google ID token cannot be empty."));
        }

        GoogleUserInfo googleUser;
        try
        {
            googleUser = await _googleAuthService.ValidateTokenAsync(command.Token, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<AuthResponse>.Failure(
                Error.Validation("Auth.GoogleTokenInvalid", $"Google token validation failed: {ex.Message}"));
        }

        if (string.IsNullOrWhiteSpace(googleUser.Email) || !googleUser.Email.Contains('@'))
        {
            return Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidGoogleToken", "Google token does not contain a valid email address."));
        }

        var normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();

        // 1. Try to find user by GoogleId
        var user = _userRepository.GetByGoogleId(googleUser.GoogleId);

        // 2. If not found by GoogleId, try to find by Email
        if (user is null)
        {
            user = _userRepository.GetByEmail(normalizedEmail);
            if (user is not null)
            {
                // Link GoogleId to existing account
                user.SetGoogleId(googleUser.GoogleId);
                _userRepository.Update(user);
            }
        }

        // 3. If still not found, create new User
        if (user is null)
        {
            var role = string.IsNullOrWhiteSpace(command.Role) ? "Student" : command.Role.Trim();
            var fullName = string.IsNullOrWhiteSpace(googleUser.Name) ? normalizedEmail.Split('@')[0] : googleUser.Name.Trim();

            user = new User(
                Guid.NewGuid(),
                normalizedEmail,
                passwordHash: null,
                fullName: fullName,
                role: role,
                googleId: googleUser.GoogleId);

            _userRepository.Add(user);

            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                if (_studentRepository.GetById(user.Id) is null)
                {
                    _studentRepository.Add(new Student(user.Id, user.FullName, user.Email));
                }
            }
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        var response = new AuthResponse(token, user.Id, user.Email, user.FullName, user.Role);

        return Result<AuthResponse>.Success(response);
    }
}
