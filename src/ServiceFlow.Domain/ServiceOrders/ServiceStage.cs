namespace ServiceFlow.Domain.ServiceOrders;

/// <summary>
/// Canonical lifecycle of a service order. Numeric values should remain stable; add new
/// stages with new numbers, never renumber existing ones (breaks persisted data).
/// </summary>
public enum ServiceStage
{
    AwaitingPickup = 0,
    PickedUp = 1,
    InService = 2,
    WaitingCustomerApproval = 3,
    Approved = 4,
    Rejected = 5,
    InCleaning = 6,
    ReadyForDelivery = 7,
    Completed = 8
}
