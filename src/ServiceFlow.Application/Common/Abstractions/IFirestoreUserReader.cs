using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Common.Abstractions;

/// <summary>
/// Reads users from Firestore by document id (same as <see cref="User.Id"/>). Used for migration verification.
/// </summary>
public interface IFirestoreUserReader
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
