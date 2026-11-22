using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Domain.Users;
using TutorMatch.Api.Infrastructure.Security;

namespace TutorMatch.Api.Features.Auth.Register;

public class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ITutorRepository? _tutorRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ITutorRepository? tutorRepository = null)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tutorRepository = tutorRepository;
    }

    public Task<Result<AuthResponse>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email) || !command.Email.Contains('@'))
        {
            return Task.FromResult(Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidEmail", "A valid email address is required.")));
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            return Task.FromResult(Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidName", "Full name is required.")));
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 6)
        {
            return Task.FromResult(Result<AuthResponse>.Failure(
                Error.Validation("Auth.InvalidPassword", "Password must be at least 6 characters long.")));
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var existingUser = _userRepository.GetByEmail(normalizedEmail);
        if (existingUser is not null)
        {
            return Task.FromResult(Result<AuthResponse>.Failure(
                Error.Conflict("Auth.EmailAlreadyExists", "An account with this email already exists.")));
        }

        var role = string.IsNullOrWhiteSpace(command.Role) ? "Student" : command.Role.Trim();
        var passwordHash = _passwordHasher.HashPassword(command.Password);

        var user = new User(
            Guid.NewGuid(),
            normalizedEmail,
            passwordHash,
            command.FullName.Trim(),
            role);

        _userRepository.Add(user);

        if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            if (_studentRepository.GetById(user.Id) is null)
            {
                _studentRepository.Add(new Student(user.Id, user.FullName, user.Email));
            }
        }
        else if (role.Equals("Tutor", StringComparison.OrdinalIgnoreCase))
        {
            if (_tutorRepository is not null && _tutorRepository.GetById(user.Id) is null)
            {
                _tutorRepository.Add(new Tutor(user.FullName, user.Email, "Tutor on TutorMatch", 50m, user.Id));
            }
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        var response = new AuthResponse(token, user.Id, user.Email, user.FullName, user.Role);

        return Task.FromResult(Result<AuthResponse>.Success(response));
    }
}
