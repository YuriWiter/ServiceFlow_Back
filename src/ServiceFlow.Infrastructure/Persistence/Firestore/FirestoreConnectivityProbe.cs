using Google.Cloud.Firestore;
using ServiceFlow.Application.Common.Abstractions;

namespace ServiceFlow.Infrastructure.Persistence.Firestore;

internal sealed class FirestoreConnectivityProbe(FirestoreDb db) : IFirestoreConnectivityProbe
{
    public async Task<FirestorePingResult> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = db.Collection(FirestoreCollections.MigrationMeta).Document("ping");
            await doc.SetAsync(new { updatedAt = FieldValue.ServerTimestamp }, cancellationToken: cancellationToken);
            var snap = await doc.GetSnapshotAsync(cancellationToken);
            return new FirestorePingResult(snap.Exists);
        }
        catch (Exception ex)
        {
            return new FirestorePingResult(false, ex.Message);
        }
    }
}
