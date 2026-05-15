namespace ServiceFlow.Application.Common.Configuration;

public enum PersistenceBackend
{
    Relational,
    Firestore
}

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public PersistenceBackend Backend { get; set; } = PersistenceBackend.Relational;
}
