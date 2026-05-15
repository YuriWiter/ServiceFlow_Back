namespace ServiceFlow.Infrastructure.Persistence.Firestore;

public sealed class FirestoreOptions
{
    public const string SectionName = "Firestore";

    public bool Enabled { get; set; }

    /// <summary>
    /// Google Cloud project id. With <c>FIRESTORE_EMULATOR_HOST</c> set, any non-empty id (e.g. <c>demo-serviceflow</c>) works.
    /// </summary>
    public string ProjectId { get; set; } = "";

    /// <summary>
    /// When true, commits mirror aggregates to Firestore after PostgreSQL (users, customers, vehicles, orders, repairs). Failures are logged only.
    /// </summary>
    public bool SyncWrites { get; set; }

    /// <summary>
    /// When true (with <see cref="Enabled"/>), login loads credentials from Firestore first, then falls back to PostgreSQL.
    /// </summary>
    public bool AuthReadFromFirestore { get; set; }
}
