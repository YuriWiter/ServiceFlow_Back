using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.RepairRequests;

public sealed class RepairRequest : AuditableEntity
{
    private readonly List<RepairMedia> _media = [];

    public Guid ServiceOrderId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string IssueDescription { get; private set; } = null!;
    public long PriceEstimateCents { get; private set; }
    public RepairUrgency Urgency { get; private set; }
    public RepairDecision? CustomerDecision { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecisionNote { get; private set; }

    public IReadOnlyCollection<RepairMedia> Media => _media.AsReadOnly();

    private RepairRequest() { }

    public static RepairRequest Create(
        Guid serviceOrderId,
        Guid createdByUserId,
        string issueDescription,
        long priceEstimateCents,
        RepairUrgency urgency)
    {
        issueDescription = Guard.NotNullOrWhiteSpace(issueDescription, DomainErrors.RepairRequest.IssueDescriptionRequired);
        Guard.MaxLength(issueDescription, 2000, DomainErrors.RepairRequest.IssueDescriptionRequired);
        Guard.NotNegative(priceEstimateCents, DomainErrors.RepairRequest.PriceEstimateNegative);

        return new RepairRequest
        {
            ServiceOrderId = serviceOrderId,
            CreatedByUserId = createdByUserId,
            IssueDescription = issueDescription,
            PriceEstimateCents = priceEstimateCents,
            Urgency = urgency
        };
    }

    public static RepairRequest FromPersistence(
        Guid id,
        Guid serviceOrderId,
        Guid createdByUserId,
        string issueDescription,
        long priceEstimateCents,
        RepairUrgency urgency,
        RepairDecision? customerDecision,
        DateTimeOffset? decidedAt,
        string? decisionNote,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<RepairMedia> media)
    {
        var r = new RepairRequest
        {
            Id = id,
            ServiceOrderId = serviceOrderId,
            CreatedByUserId = createdByUserId,
            IssueDescription = issueDescription,
            PriceEstimateCents = priceEstimateCents,
            Urgency = urgency,
            CustomerDecision = customerDecision,
            DecidedAt = decidedAt,
            DecisionNote = decisionNote,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        foreach (var m in media)
        {
            r._media.Add(m);
        }

        return r;
    }

    public RepairMedia AttachMedia(MediaType mediaType, string storagePath, string? mimeType, long sizeBytes)
    {
        var media = RepairMedia.Attach(Id, mediaType, storagePath, mimeType, sizeBytes);
        _media.Add(media);
        Touch();
        return media;
    }

    public void UpdateEstimate(string issueDescription, long priceEstimateCents, RepairUrgency urgency)
    {
        if (CustomerDecision is not null)
        {
            throw new DomainException(
                DomainErrors.RepairRequest.AlreadyDecided,
                "Cannot modify an estimate after the customer has decided.");
        }

        IssueDescription = Guard.NotNullOrWhiteSpace(issueDescription, DomainErrors.RepairRequest.IssueDescriptionRequired);
        PriceEstimateCents = Guard.NotNegative(priceEstimateCents, DomainErrors.RepairRequest.PriceEstimateNegative);
        Urgency = urgency;
        Touch();
    }

    public void RegisterDecision(RepairDecision decision, string? note, DateTimeOffset? now = null)
    {
        if (CustomerDecision is not null)
        {
            throw new DomainException(
                DomainErrors.RepairRequest.AlreadyDecided,
                "This repair has already been decided.");
        }

        if (_media.Count == 0)
        {
            throw new DomainException(
                DomainErrors.RepairRequest.MediaRequired,
                "Customer decision requires at least one piece of media as proof.");
        }

        CustomerDecision = decision;
        DecidedAt = now ?? DateTimeOffset.UtcNow;
        DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Touch(now);
    }

    public bool IsDecided => CustomerDecision is not null;
}
