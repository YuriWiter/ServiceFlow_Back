using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.ServiceOrders;

public sealed class ServiceStatusHistoryEntry : Entity
{
    public Guid ServiceOrderId { get; private set; }
    public ServiceStage? FromStage { get; private set; }
    public ServiceStage ToStage { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public string? Note { get; private set; }

    private ServiceStatusHistoryEntry() { }

    internal static ServiceStatusHistoryEntry Record(
        Guid serviceOrderId,
        ServiceStage? fromStage,
        ServiceStage toStage,
        Guid changedByUserId,
        string? note,
        DateTimeOffset? now = null)
    {
        return new ServiceStatusHistoryEntry
        {
            ServiceOrderId = serviceOrderId,
            FromStage = fromStage,
            ToStage = toStage,
            ChangedByUserId = changedByUserId,
            ChangedAt = now ?? DateTimeOffset.UtcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }

    public static ServiceStatusHistoryEntry FromPersistence(
        Guid id,
        Guid serviceOrderId,
        ServiceStage? fromStage,
        ServiceStage toStage,
        Guid changedByUserId,
        DateTimeOffset changedAt,
        string? note)
    {
        return new ServiceStatusHistoryEntry
        {
            Id = id,
            ServiceOrderId = serviceOrderId,
            FromStage = fromStage,
            ToStage = toStage,
            ChangedByUserId = changedByUserId,
            ChangedAt = changedAt,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }
}
