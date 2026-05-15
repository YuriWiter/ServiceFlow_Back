using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Infrastructure.Persistence.Firestore;

internal sealed class FirestoreSyncService(FirestoreDb db, ILogger<FirestoreSyncService> logger) : IFirestoreSyncService
{
    public async Task UpsertUserAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = db.Collection(FirestoreCollections.Users).Document(user.Id.ToString());
            var data = new Dictionary<string, object>
            {
                ["email"] = user.Email,
                ["fullName"] = user.FullName,
                ["passwordHash"] = user.PasswordHash,
                ["role"] = user.Role.ToString(),
                ["isActive"] = user.IsActive,
                ["createdAt"] = Timestamp.FromDateTime(user.CreatedAt.UtcDateTime),
                ["updatedAt"] = Timestamp.FromDateTime(user.UpdatedAt.UtcDateTime)
            };
            await doc.SetAsync(data, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firestore user upsert failed for {UserId}", user.Id);
        }
    }

    public async Task UpsertCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = db.Collection(FirestoreCollections.Customers).Document(customer.Id.ToString());
            var data = new Dictionary<string, object>
            {
                ["fullName"] = customer.FullName,
                ["phoneNumber"] = customer.PhoneNumber,
                ["email"] = customer.Email ?? string.Empty,
                ["userId"] = customer.UserId?.ToString() ?? string.Empty,
                ["createdAt"] = Timestamp.FromDateTime(customer.CreatedAt.UtcDateTime),
                ["updatedAt"] = Timestamp.FromDateTime(customer.UpdatedAt.UtcDateTime)
            };
            await doc.SetAsync(data, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firestore customer upsert failed for {CustomerId}", customer.Id);
        }
    }

    public async Task UpsertVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        try
        {
            var doc = db.Collection(FirestoreCollections.Vehicles).Document(vehicle.Id.ToString());
            var data = new Dictionary<string, object>
            {
                ["customerId"] = vehicle.CustomerId.ToString(),
                ["licensePlate"] = vehicle.LicensePlate,
                ["make"] = vehicle.Make,
                ["model"] = vehicle.Model,
                ["year"] = vehicle.Year,
                ["color"] = vehicle.Color ?? string.Empty,
                ["vin"] = vehicle.Vin ?? string.Empty,
                ["createdAt"] = Timestamp.FromDateTime(vehicle.CreatedAt.UtcDateTime),
                ["updatedAt"] = Timestamp.FromDateTime(vehicle.UpdatedAt.UtcDateTime)
            };
            await doc.SetAsync(data, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firestore vehicle upsert failed for {VehicleId}", vehicle.Id);
        }
    }

    public async Task UpsertServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default)
    {
        try
        {
            var historyMaps = order.StatusHistory
                .Select(e => new Dictionary<string, object>
                {
                    ["id"] = e.Id.ToString(),
                    ["serviceOrderId"] = e.ServiceOrderId.ToString(),
                    ["fromStage"] = e.FromStage?.ToString() ?? string.Empty,
                    ["toStage"] = e.ToStage.ToString(),
                    ["changedByUserId"] = e.ChangedByUserId.ToString(),
                    ["changedAt"] = Timestamp.FromDateTime(e.ChangedAt.UtcDateTime),
                    ["note"] = e.Note ?? string.Empty
                })
                .Cast<object>()
                .ToList();

            var repairIds = order.RepairRequests.Select(r => r.Id.ToString()).ToList();

            var doc = db.Collection(FirestoreCollections.ServiceOrders).Document(order.Id.ToString());
            var data = new Dictionary<string, object>
            {
                ["customerId"] = order.CustomerId.ToString(),
                ["vehicleId"] = order.VehicleId.ToString(),
                ["assignedStaffId"] = order.AssignedStaffId?.ToString() ?? string.Empty,
                ["stage"] = order.Stage.ToString(),
                ["openingDescription"] = order.OpeningDescription,
                ["createdAt"] = Timestamp.FromDateTime(order.CreatedAt.UtcDateTime),
                ["updatedAt"] = Timestamp.FromDateTime(order.UpdatedAt.UtcDateTime),
                ["statusHistory"] = historyMaps,
                ["repairRequestIds"] = repairIds
            };

            if (order.CompletedAt is { } completed)
            {
                data["completedAt"] = Timestamp.FromDateTime(completed.UtcDateTime);
            }

            await doc.SetAsync(data, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firestore service order upsert failed for {ServiceOrderId}", order.Id);
        }
    }

    public async Task UpsertRepairRequestAsync(RepairRequest repair, CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaMaps = repair.Media
                .Select(m => new Dictionary<string, object>
                {
                    ["id"] = m.Id.ToString(),
                    ["repairRequestId"] = m.RepairRequestId.ToString(),
                    ["mediaType"] = m.MediaType.ToString(),
                    ["storagePath"] = m.StoragePath,
                    ["mimeType"] = m.MimeType ?? string.Empty,
                    ["sizeBytes"] = m.SizeBytes,
                    ["uploadedAt"] = Timestamp.FromDateTime(m.UploadedAt.UtcDateTime)
                })
                .Cast<object>()
                .ToList();

            var doc = db.Collection(FirestoreCollections.RepairRequests).Document(repair.Id.ToString());
            var data = new Dictionary<string, object>
            {
                ["serviceOrderId"] = repair.ServiceOrderId.ToString(),
                ["createdByUserId"] = repair.CreatedByUserId.ToString(),
                ["issueDescription"] = repair.IssueDescription,
                ["priceEstimateCents"] = repair.PriceEstimateCents,
                ["urgency"] = repair.Urgency.ToString(),
                ["createdAt"] = Timestamp.FromDateTime(repair.CreatedAt.UtcDateTime),
                ["updatedAt"] = Timestamp.FromDateTime(repair.UpdatedAt.UtcDateTime),
                ["media"] = mediaMaps
            };

            if (repair.CustomerDecision is { } decision)
            {
                data["customerDecision"] = decision.ToString();
            }

            if (repair.DecidedAt is { } decidedAt)
            {
                data["decidedAt"] = Timestamp.FromDateTime(decidedAt.UtcDateTime);
            }

            if (!string.IsNullOrEmpty(repair.DecisionNote))
            {
                data["decisionNote"] = repair.DecisionNote;
            }

            await doc.SetAsync(data, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Firestore repair request upsert failed for {RepairRequestId}", repair.Id);
        }
    }
}
