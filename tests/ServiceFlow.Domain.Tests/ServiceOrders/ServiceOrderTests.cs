using FluentAssertions;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;

namespace ServiceFlow.Domain.Tests.ServiceOrders;

public sealed class ServiceOrderTests
{
    private static readonly Guid StaffId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid VehicleId = Guid.NewGuid();

    private static ServiceOrder NewOrder() =>
        ServiceOrder.Open(CustomerId, VehicleId, "Brake noise on left side", StaffId);

    [Fact]
    public void Open_creates_order_in_awaiting_pickup_and_records_history()
    {
        var order = NewOrder();

        order.Stage.Should().Be(ServiceStage.AwaitingPickup);
        order.StatusHistory.Should().HaveCount(1);
        order.StatusHistory.Single().ToStage.Should().Be(ServiceStage.AwaitingPickup);
    }

    [Fact]
    public void Transition_honors_state_machine()
    {
        var order = NewOrder();

        order.TransitionTo(ServiceStage.PickedUp, StaffId);
        order.TransitionTo(ServiceStage.InService, StaffId);

        order.Stage.Should().Be(ServiceStage.InService);
        order.StatusHistory.Should().HaveCount(3);
    }

    [Fact]
    public void Skipping_stages_throws_domain_exception()
    {
        var order = NewOrder();

        var act = () => order.TransitionTo(ServiceStage.InService, StaffId);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.ServiceOrder.InvalidStageTransition);
    }

    [Fact]
    public void Transition_sets_completed_at_when_completed()
    {
        var order = NewOrder();
        order.TransitionTo(ServiceStage.PickedUp, StaffId);
        order.TransitionTo(ServiceStage.InService, StaffId);
        order.TransitionTo(ServiceStage.WaitingCustomerApproval, StaffId);
        order.TransitionTo(ServiceStage.Approved, StaffId);
        order.TransitionTo(ServiceStage.InCleaning, StaffId);
        order.TransitionTo(ServiceStage.ReadyForDelivery, StaffId);
        order.TransitionTo(ServiceStage.Completed, StaffId);

        order.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cannot_leave_waiting_approval_when_repairs_are_undecided()
    {
        var order = NewOrder();
        order.TransitionTo(ServiceStage.PickedUp, StaffId);
        order.TransitionTo(ServiceStage.InService, StaffId);
        order.TransitionTo(ServiceStage.WaitingCustomerApproval, StaffId);

        order.AddRepairRequest(StaffId, "Replace pad", 15000, RepairUrgency.High);

        var act = () => order.TransitionTo(ServiceStage.Approved, StaffId);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.ServiceOrder.CannotTransitionBeforeApproval);
    }

    [Fact]
    public void Completed_order_cannot_transition()
    {
        var order = NewOrder();
        foreach (var next in new[]
        {
            ServiceStage.PickedUp,
            ServiceStage.InService,
            ServiceStage.WaitingCustomerApproval,
            ServiceStage.Approved,
            ServiceStage.InCleaning,
            ServiceStage.ReadyForDelivery,
            ServiceStage.Completed
        })
        {
            order.TransitionTo(next, StaffId);
        }

        var act = () => order.TransitionTo(ServiceStage.AwaitingPickup, StaffId);
        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.ServiceOrder.AlreadyCompleted);
    }
}
