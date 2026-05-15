using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Infrastructure.Persistence.Firestore;

internal sealed class NoOpFirestoreUserReader : IFirestoreUserReader
{
    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);
}
