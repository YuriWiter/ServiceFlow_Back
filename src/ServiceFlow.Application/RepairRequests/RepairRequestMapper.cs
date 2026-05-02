using ServiceFlow.Domain.RepairRequests;

namespace ServiceFlow.Application.RepairRequests;

internal static class RepairRequestMapper
{
    public static RepairRequestDto ToDto(RepairRequest r)
    {
        return new RepairRequestDto(
            r.Id,
            r.ServiceOrderId,
            r.CreatedByUserId,
            r.IssueDescription,
            r.PriceEstimateCents,
            r.Urgency,
            r.CustomerDecision,
            r.DecidedAt,
            r.DecisionNote,
            r.Media
                .OrderBy(m => m.UploadedAt)
                .Select(m => new RepairMediaDto(m.Id, m.MediaType, m.StoragePath, m.MimeType, m.SizeBytes, m.UploadedAt))
                .ToList(),
            r.CreatedAt,
            r.UpdatedAt);
    }
}
