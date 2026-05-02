namespace ServiceFlow.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    protected void Touch(DateTimeOffset? now = null)
    {
        UpdatedAt = now ?? DateTimeOffset.UtcNow;
    }
}
