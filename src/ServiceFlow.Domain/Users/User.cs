using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.Users;

public sealed class User : AuditableEntity
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    private User() { }

    public static User Create(string email, string fullName, string passwordHash, UserRole role)
    {
        email = Guard.NotNullOrWhiteSpace(email, DomainErrors.User.EmailRequired).ToLowerInvariant();
        Guard.MaxLength(email, 255, DomainErrors.User.EmailTooLong);

        if (!email.Contains('@') || email.Length < 5)
        {
            throw new DomainException(DomainErrors.User.EmailInvalid, "Invalid email format.");
        }

        fullName = Guard.NotNullOrWhiteSpace(fullName, DomainErrors.User.FullNameRequired);
        Guard.MaxLength(fullName, 200, DomainErrors.User.FullNameTooLong);
        Guard.NotNullOrWhiteSpace(passwordHash, DomainErrors.User.PasswordHashRequired);

        return new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true
        };
    }

    public void UpdateProfile(string fullName)
    {
        FullName = Guard.NotNullOrWhiteSpace(fullName, DomainErrors.User.FullNameRequired);
        Touch();
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = Guard.NotNullOrWhiteSpace(newPasswordHash, DomainErrors.User.PasswordHashRequired);
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Rehydrates a user from persistence (PostgreSQL, Firestore, etc.). Assumes stored data is already valid.
    /// </summary>
    public static User FromPersistence(
        Guid id,
        string email,
        string fullName,
        string passwordHash,
        UserRole role,
        bool isActive,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new User
        {
            Id = id,
            Email = email,
            FullName = fullName,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
