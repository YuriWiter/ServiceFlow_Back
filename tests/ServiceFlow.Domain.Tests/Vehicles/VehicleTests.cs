using FluentAssertions;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Domain.Tests.Vehicles;

public sealed class VehicleTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public void Normalizes_license_plate_uppercased_and_trimmed()
    {
        var vehicle = Vehicle.Create(CustomerId, "abc-1d23", "Honda", "Civic", 2020);
        vehicle.LicensePlate.Should().Be("ABC1D23");
    }

    [Theory]
    [InlineData("", "vehicle.license_plate.required")]
    [InlineData("ab", "vehicle.license_plate.invalid")]
    [InlineData("AAAAAAAAAAAAAAA", "vehicle.license_plate.invalid")]
    public void Invalid_license_plate_throws(string plate, string expectedCode)
    {
        var act = () => Vehicle.Create(CustomerId, plate, "Honda", "Civic", 2020);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Year_outside_range_throws()
    {
        var act = () => Vehicle.Create(CustomerId, "AAA1234", "Honda", "Civic", 1800);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(DomainErrors.Vehicle.YearOutOfRange);
    }

    [Fact]
    public void Update_touches_updated_at_and_updates_details()
    {
        var vehicle = Vehicle.Create(CustomerId, "AAA1234", "Honda", "Civic", 2020, "Black");
        var before = vehicle.UpdatedAt;

        Thread.Sleep(5);
        vehicle.UpdateDetails("Honda", "Civic", 2021, "Red", "1HGCM82633A123456");

        vehicle.Year.Should().Be(2021);
        vehicle.Color.Should().Be("Red");
        vehicle.Vin.Should().Be("1HGCM82633A123456");
        vehicle.UpdatedAt.Should().BeAfter(before);
    }
}
