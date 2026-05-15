namespace ServiceFlow.Application.Common.Abstractions;

/// <summary>
/// Feature flags for the PostgreSQL → Firestore cutover (backed by <c>Firestore</c> configuration).
/// </summary>
public interface IFirestoreMigrationSettings
{
    /// <summary>
    /// When true (and Firestore is enabled), login resolves the user from Firestore first, then falls back to PostgreSQL.
    /// </summary>
    bool AuthReadFromFirestore { get; }
}
