using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.Customers;

namespace ServiceFlow.Domain.Vehicles;

public sealed class Vehicle : AuditableEntity
{
    private const int MinYear = 1900;

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;

    public string LicensePlate { get; private set; } = null!;
    public string Make { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public int Year { get; private set; }
    public string? Color { get; private set; }
    public string? Vin { get; private set; }

    private Vehicle() { }

    public static Vehicle Create(
        Guid customerId,
        string licensePlate,
        string make,
        string model,
        int year,
        string? color = null,
        string? vin = null)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("vehicle.customer_id.required", "Customer id is required.");
        }

        licensePlate = NormalizeLicensePlate(licensePlate);

        make = Guard.NotNullOrWhiteSpace(make, DomainErrors.Vehicle.MakeRequired);
        Guard.MaxLength(make, 60, DomainErrors.Vehicle.MakeRequired);
        model = Guard.NotNullOrWhiteSpace(model, DomainErrors.Vehicle.ModelRequired);
        Guard.MaxLength(model, 60, DomainErrors.Vehicle.ModelRequired);

        var currentYear = DateTime.UtcNow.Year;
        Guard.InRange(year, MinYear, currentYear + 1, DomainErrors.Vehicle.YearOutOfRange);

        return new Vehicle
        {
            CustomerId = customerId,
            LicensePlate = licensePlate,
            Make = make,
            Model = model,
            Year = year,
            Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            Vin = string.IsNullOrWhiteSpace(vin) ? null : vin.Trim().ToUpperInvariant()
        };
    }

    public static Vehicle FromPersistence(
        Guid id,
        Guid customerId,
        string licensePlate,
        string make,
        string model,
        int year,
        string? color,
        string? vin,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new Vehicle
        {
            Id = id,
            CustomerId = customerId,
            Customer = null!,
            LicensePlate = licensePlate,
            Make = make,
            Model = model,
            Year = year,
            Color = color,
            Vin = vin,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void UpdateDetails(string make, string model, int year, string? color, string? vin)
    {
        Make = Guard.NotNullOrWhiteSpace(make, DomainErrors.Vehicle.MakeRequired);
        Model = Guard.NotNullOrWhiteSpace(model, DomainErrors.Vehicle.ModelRequired);
        var currentYear = DateTime.UtcNow.Year;
        Year = Guard.InRange(year, MinYear, currentYear + 1, DomainErrors.Vehicle.YearOutOfRange);
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        Vin = string.IsNullOrWhiteSpace(vin) ? null : vin.Trim().ToUpperInvariant();
        Touch();
    }

    private static string NormalizeLicensePlate(string licensePlate)
    {
        var value = Guard.NotNullOrWhiteSpace(licensePlate, DomainErrors.Vehicle.LicensePlateRequired)
            .ToUpperInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty);

        if (value.Length is < 4 or > 10)
        {
            throw new DomainException(DomainErrors.Vehicle.LicensePlateInvalid, "License plate must be 4-10 chars.");
        }

        return value;
    }
}
