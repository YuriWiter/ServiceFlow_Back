namespace ServiceFlow.Application.Common.Abstractions;

/// <summary>
/// Optional probe used while migrating from PostgreSQL to Firestore. Not registered unless <c>Firestore:Enabled</c> is true.
/// </summary>
public interface IFirestoreConnectivityProbe
{
    Task<FirestorePingResult> PingAsync(CancellationToken cancellationToken = default);
}

public sealed record FirestorePingResult(bool Ok, string? Error = null);
