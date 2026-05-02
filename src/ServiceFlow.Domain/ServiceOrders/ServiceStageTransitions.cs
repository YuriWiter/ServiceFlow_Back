namespace ServiceFlow.Domain.ServiceOrders;

/// <summary>
/// Single source of truth for the service-order state machine. Mirrors the Postgres
/// constraints and the frontend domain map so transitions cannot skip stages.
/// </summary>
public static class ServiceStageTransitions
{
    private static readonly Dictionary<ServiceStage, IReadOnlySet<ServiceStage>> _map = new()
    {
        [ServiceStage.AwaitingPickup] = new HashSet<ServiceStage> { ServiceStage.PickedUp },
        [ServiceStage.PickedUp] = new HashSet<ServiceStage> { ServiceStage.InService },
        [ServiceStage.InService] = new HashSet<ServiceStage> { ServiceStage.WaitingCustomerApproval },
        [ServiceStage.WaitingCustomerApproval] = new HashSet<ServiceStage> { ServiceStage.Approved, ServiceStage.Rejected },
        [ServiceStage.Approved] = new HashSet<ServiceStage> { ServiceStage.InCleaning },
        [ServiceStage.Rejected] = new HashSet<ServiceStage> { ServiceStage.InCleaning },
        [ServiceStage.InCleaning] = new HashSet<ServiceStage> { ServiceStage.ReadyForDelivery },
        [ServiceStage.ReadyForDelivery] = new HashSet<ServiceStage> { ServiceStage.Completed },
        [ServiceStage.Completed] = new HashSet<ServiceStage>()
    };

    public static IReadOnlySet<ServiceStage> AllowedNext(ServiceStage current)
        => _map.TryGetValue(current, out var next) ? next : new HashSet<ServiceStage>();

    public static bool CanTransition(ServiceStage from, ServiceStage to)
        => AllowedNext(from).Contains(to);

    /// <summary>
    /// Stages that the customer can see (and therefore that appear in the customer timeline).
    /// </summary>
    public static IReadOnlyList<ServiceStage> AllStagesInOrder { get; } =
    [
        ServiceStage.AwaitingPickup,
        ServiceStage.PickedUp,
        ServiceStage.InService,
        ServiceStage.WaitingCustomerApproval,
        ServiceStage.Approved,
        ServiceStage.InCleaning,
        ServiceStage.ReadyForDelivery,
        ServiceStage.Completed
    ];

    /// <summary>
    /// Stages that require a pending customer decision before the workflow can advance.
    /// </summary>
    public static bool RequiresCustomerDecision(ServiceStage stage) =>
        stage == ServiceStage.WaitingCustomerApproval;
}
