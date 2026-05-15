using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.RepairRequests;

namespace ServiceFlow.Domain.ServiceOrders;

public sealed class ServiceOrder : AuditableEntity
{
    private readonly List<ServiceStatusHistoryEntry> _statusHistory = [];
    private readonly List<RepairRequest> _repairRequests = [];

    public Guid CustomerId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid? AssignedStaffId { get; private set; }

    public ServiceStage Stage { get; private set; } = ServiceStage.AwaitingPickup;
    public string OpeningDescription { get; private set; } = null!;
    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<ServiceStatusHistoryEntry> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<RepairRequest> RepairRequests => _repairRequests.AsReadOnly();

    private ServiceOrder() { }

    public static ServiceOrder Open(
        Guid customerId,
        Guid vehicleId,
        string openingDescription,
        Guid openedByUserId,
        Guid? assignedStaffId = null,
        DateTimeOffset? now = null)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("service_order.customer_id.required", "Customer id is required.");
        }

        if (vehicleId == Guid.Empty)
        {
            throw new DomainException("service_order.vehicle_id.required", "Vehicle id is required.");
        }

        openingDescription = Guard.NotNullOrWhiteSpace(openingDescription, DomainErrors.ServiceOrder.OpeningDescriptionRequired);
        Guard.MaxLength(openingDescription, 2000, DomainErrors.ServiceOrder.OpeningDescriptionTooLong);

        var order = new ServiceOrder
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            AssignedStaffId = assignedStaffId,
            OpeningDescription = openingDescription,
            Stage = ServiceStage.AwaitingPickup
        };

        order._statusHistory.Add(ServiceStatusHistoryEntry.Record(
            order.Id,
            fromStage: null,
            toStage: ServiceStage.AwaitingPickup,
            changedByUserId: openedByUserId,
            note: "Service order opened.",
            now: now));

        return order;
    }

    public static ServiceOrder FromPersistence(
        Guid id,
        Guid customerId,
        Guid vehicleId,
        Guid? assignedStaffId,
        ServiceStage stage,
        string openingDescription,
        DateTimeOffset? completedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<ServiceStatusHistoryEntry> history,
        IEnumerable<RepairRequest> repairs)
    {
        var order = new ServiceOrder
        {
            Id = id,
            CustomerId = customerId,
            VehicleId = vehicleId,
            AssignedStaffId = assignedStaffId,
            Stage = stage,
            OpeningDescription = openingDescription,
            CompletedAt = completedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        order._statusHistory.AddRange(history);
        order._repairRequests.AddRange(repairs);

        return order;
    }

    public void AssignStaff(Guid staffUserId)
    {
        if (staffUserId == Guid.Empty)
        {
            throw new DomainException("service_order.staff.required", "Staff id is required.");
        }

        AssignedStaffId = staffUserId;
        Touch();
    }

    public void TransitionTo(ServiceStage next, Guid changedByUserId, string? note = null, DateTimeOffset? now = null)
    {
        if (Stage == ServiceStage.Completed)
        {
            throw new DomainException(DomainErrors.ServiceOrder.AlreadyCompleted, "Service order is already completed.");
        }

        if (!ServiceStageTransitions.CanTransition(Stage, next))
        {
            throw new DomainException(
                DomainErrors.ServiceOrder.InvalidStageTransition,
                $"Cannot transition from {Stage} to {next}.");
        }

        // Leaving WaitingCustomerApproval requires that every pending repair has been decided.
        if (Stage == ServiceStage.WaitingCustomerApproval && _repairRequests.Any(r => !r.IsDecided))
        {
            throw new DomainException(
                DomainErrors.ServiceOrder.CannotTransitionBeforeApproval,
                "All repair requests must have a customer decision before advancing.");
        }

        var previous = Stage;
        Stage = next;

        if (next == ServiceStage.Completed)
        {
            CompletedAt = now ?? DateTimeOffset.UtcNow;
        }

        _statusHistory.Add(ServiceStatusHistoryEntry.Record(
            Id,
            fromStage: previous,
            toStage: next,
            changedByUserId: changedByUserId,
            note: note,
            now: now));

        Touch(now);
    }

    public RepairRequest AddRepairRequest(
        Guid createdByUserId,
        string issueDescription,
        long priceEstimateCents,
        RepairUrgency urgency)
    {
        if (Stage is ServiceStage.Completed or ServiceStage.ReadyForDelivery)
        {
            throw new DomainException(
                "service_order.cannot_add_repair_at_stage",
                $"Cannot add repair requests while stage is {Stage}.");
        }

        var repair = RepairRequest.Create(Id, createdByUserId, issueDescription, priceEstimateCents, urgency);
        _repairRequests.Add(repair);
        Touch();
        return repair;
    }

    public void RegisterRepairDecision(Guid repairRequestId, RepairDecision decision, string? note, DateTimeOffset? now = null)
    {
        var repair = _repairRequests.FirstOrDefault(r => r.Id == repairRequestId)
            ?? throw new DomainException("repair_request.not_found", "Repair request does not belong to this order.");

        repair.RegisterDecision(decision, note, now);
        Touch(now);
    }
}
