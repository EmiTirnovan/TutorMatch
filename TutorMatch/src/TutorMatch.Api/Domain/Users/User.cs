namespace TutorMatch.Api.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Student";
    public string? GoogleId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // EF Core

    public User(
        Guid id,
        string email,
        string? passwordHash,
        string fullName,
        string role = "Student",
        string? googleId = null,
        DateTime? createdAt = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        if (!email.Contains('@'))
        {
            throw new ArgumentException("Invalid email format.", nameof(email));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        ArgumentException.ThrowIfNullOrWhiteSpace(role, nameof(role));

        Id = id;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Role = role.Trim();
        GoogleId = string.IsNullOrWhiteSpace(googleId) ? null : googleId.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    public void SetGoogleId(string googleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(googleId, nameof(googleId));
        GoogleId = googleId.Trim();
    }

    public void UpdateProfile(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        FullName = fullName.Trim();
    }
}
