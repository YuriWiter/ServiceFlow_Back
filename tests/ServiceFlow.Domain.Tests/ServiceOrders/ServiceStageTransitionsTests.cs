using FluentAssertions;
using ServiceFlow.Domain.ServiceOrders;

namespace ServiceFlow.Domain.Tests.ServiceOrders;

public sealed class ServiceStageTransitionsTests
{
    [Theory]
    [InlineData(ServiceStage.AwaitingPickup, ServiceStage.PickedUp, true)]
    [InlineData(ServiceStage.PickedUp, ServiceStage.InService, true)]
    [InlineData(ServiceStage.InService, ServiceStage.WaitingCustomerApproval, true)]
    [InlineData(ServiceStage.WaitingCustomerApproval, ServiceStage.Approved, true)]
    [InlineData(ServiceStage.WaitingCustomerApproval, ServiceStage.Rejected, true)]
    [InlineData(ServiceStage.Approved, ServiceStage.InCleaning, true)]
    [InlineData(ServiceStage.Rejected, ServiceStage.InCleaning, true)]
    [InlineData(ServiceStage.InCleaning, ServiceStage.ReadyForDelivery, true)]
    [InlineData(ServiceStage.ReadyForDelivery, ServiceStage.Completed, true)]
    public void Valid_transitions_are_allowed(ServiceStage from, ServiceStage to, bool expected)
    {
        ServiceStageTransitions.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(ServiceStage.AwaitingPickup, ServiceStage.InService)]
    [InlineData(ServiceStage.PickedUp, ServiceStage.Completed)]
    [InlineData(ServiceStage.WaitingCustomerApproval, ServiceStage.InCleaning)]
    [InlineData(ServiceStage.Completed, ServiceStage.AwaitingPickup)]
    [InlineData(ServiceStage.InService, ServiceStage.Approved)]
    public void Invalid_transitions_are_rejected(ServiceStage from, ServiceStage to)
    {
        ServiceStageTransitions.CanTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void Completed_stage_has_no_next()
    {
        ServiceStageTransitions.AllowedNext(ServiceStage.Completed).Should().BeEmpty();
    }

    [Fact]
    public void All_known_stages_are_reachable_from_initial_state()
    {
        var visited = new HashSet<ServiceStage> { ServiceStage.AwaitingPickup };
        var frontier = new Queue<ServiceStage>();
        frontier.Enqueue(ServiceStage.AwaitingPickup);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var next in ServiceStageTransitions.AllowedNext(current))
            {
                if (visited.Add(next))
                {
                    frontier.Enqueue(next);
                }
            }
        }

        visited.Should().Contain(Enum.GetValues<ServiceStage>());
    }
}
