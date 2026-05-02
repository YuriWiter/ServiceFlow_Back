using ServiceFlow.Domain.RepairRequests;

namespace ServiceFlow.Application.RepairRequests;

public sealed record CreateRepairRequestCommand(
    Guid ServiceOrderId,
    string IssueDescription,
    long PriceEstimateCents,
    RepairUrgency Urgency);

public sealed record UpdateRepairEstimateCommand(
    Guid RepairRequestId,
    string IssueDescription,
    long PriceEstimateCents,
    RepairUrgency Urgency);

public sealed record AttachRepairMediaCommand(
    Guid RepairRequestId,
    MediaType MediaType,
    string StoragePath,
    string? MimeType,
    long SizeBytes);

public sealed record RegisterCustomerDecisionCommand(
    Guid RepairRequestId,
    RepairDecision Decision,
    string? Note);

public sealed record RepairMediaDto(
    Guid Id,
    MediaType MediaType,
    string StoragePath,
    string? MimeType,
    long SizeBytes,
    DateTimeOffset UploadedAt);

public sealed record RepairRequestDto(
    Guid Id,
    Guid ServiceOrderId,
    Guid CreatedByUserId,
    string IssueDescription,
    long PriceEstimateCents,
    RepairUrgency Urgency,
    RepairDecision? CustomerDecision,
    DateTimeOffset? DecidedAt,
    string? DecisionNote,
    IReadOnlyList<RepairMediaDto> Media,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
