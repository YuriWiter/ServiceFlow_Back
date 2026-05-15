using Google.Cloud.Firestore;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Infrastructure.Persistence.Firestore;

internal sealed class FirestoreUserReader(FirestoreDb db) : IFirestoreUserReader
{
    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var snap = await db.Collection(FirestoreCollections.Users)
            .Document(userId.ToString())
            .GetSnapshotAsync(cancellationToken);
        if (!snap.Exists)
            return null;
        return FirestoreUserMapper.ToUser(snap);
    }

    public async Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var query = db.Collection(FirestoreCollections.Users)
            .WhereEqualTo("email", normalizedEmail)
            .Limit(1);
        var snap = await query.GetSnapshotAsync(cancellationToken);
        if (snap.Count == 0)
            return null;
        return FirestoreUserMapper.ToUser(snap.Documents[0]);
    }
}
