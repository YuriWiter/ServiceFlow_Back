using FluentAssertions;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.RepairRequests;

namespace ServiceFlow.Domain.Tests.RepairRequests;

public sealed class RepairRequestTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid StaffId = Guid.NewGuid();

    private static RepairRequest NewRepair()
        => RepairRequest.Create(OrderId, StaffId, "Replace filter", 8000, RepairUrgency.Medium);

    [Fact]
    public void Create_requires_non_negative_price()
    {
        var act = () => RepairRequest.Create(OrderId, StaffId, "x", -1, RepairUrgency.Low);
        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.RepairRequest.PriceEstimateNegative);
    }

    [Fact]
    public void Decision_requires_at_least_one_media()
    {
        var repair = NewRepair();

        var act = () => repair.RegisterDecision(RepairDecision.Approved, null);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.RepairRequest.MediaRequired);
    }

    [Fact]
    public void Decision_succeeds_when_media_is_attached()
    {
        var repair = NewRepair();
        repair.AttachMedia(MediaType.Photo, "repair/photo.jpg", "image/jpeg", 1234);

        repair.RegisterDecision(RepairDecision.Approved, "Go ahead.");

        repair.IsDecided.Should().BeTrue();
        repair.CustomerDecision.Should().Be(RepairDecision.Approved);
        repair.DecidedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cannot_decide_twice()
    {
        var repair = NewRepair();
        repair.AttachMedia(MediaType.Photo, "repair/photo.jpg", "image/jpeg", 1234);
        repair.RegisterDecision(RepairDecision.Approved, null);

        var act = () => repair.RegisterDecision(RepairDecision.Rejected, null);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.RepairRequest.AlreadyDecided);
    }

    [Fact]
    public void Cannot_update_estimate_after_decision()
    {
        var repair = NewRepair();
        repair.AttachMedia(MediaType.Photo, "repair/photo.jpg", "image/jpeg", 1234);
        repair.RegisterDecision(RepairDecision.Approved, null);

        var act = () => repair.UpdateEstimate("x", 500, RepairUrgency.Low);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(DomainErrors.RepairRequest.AlreadyDecided);
    }
}
