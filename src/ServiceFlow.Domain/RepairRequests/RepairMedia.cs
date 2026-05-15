using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.RepairRequests;

public sealed class RepairMedia : Entity
{
    public Guid RepairRequestId { get; private set; }
    public MediaType MediaType { get; private set; }
    public string StoragePath { get; private set; } = null!;
    public string? MimeType { get; private set; }
    public long SizeBytes { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    private RepairMedia() { }

    internal static RepairMedia Attach(
        Guid repairRequestId,
        MediaType mediaType,
        string storagePath,
        string? mimeType,
        long sizeBytes,
        DateTimeOffset? now = null)
    {
        storagePath = Guard.NotNullOrWhiteSpace(storagePath, DomainErrors.RepairRequest.InvalidMediaPath);
        Guard.MaxLength(storagePath, 500, DomainErrors.RepairRequest.InvalidMediaPath);
        Guard.NotNegative(sizeBytes, "repair_request.media.size_negative");

        return new RepairMedia
        {
            RepairRequestId = repairRequestId,
            MediaType = mediaType,
            StoragePath = storagePath,
            MimeType = string.IsNullOrWhiteSpace(mimeType) ? null : mimeType.Trim(),
            SizeBytes = sizeBytes,
            UploadedAt = now ?? DateTimeOffset.UtcNow
        };
    }

    public static RepairMedia FromPersistence(
        Guid id,
        Guid repairRequestId,
        MediaType mediaType,
        string storagePath,
        string? mimeType,
        long sizeBytes,
        DateTimeOffset uploadedAt)
    {
        return new RepairMedia(id, repairRequestId, mediaType, storagePath, mimeType, sizeBytes, uploadedAt);
    }

    private RepairMedia(
        Guid id,
        Guid repairRequestId,
        MediaType mediaType,
        string storagePath,
        string? mimeType,
        long sizeBytes,
        DateTimeOffset uploadedAt)
    {
        Id = id;
        RepairRequestId = repairRequestId;
        MediaType = mediaType;
        StoragePath = storagePath;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        UploadedAt = uploadedAt;
    }
}
