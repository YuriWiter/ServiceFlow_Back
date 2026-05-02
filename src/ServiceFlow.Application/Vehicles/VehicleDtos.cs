namespace ServiceFlow.Application.Vehicles;

public sealed record CreateVehicleCommand(
    Guid CustomerId,
    string LicensePlate,
    string Make,
    string Model,
    int Year,
    string? Color,
    string? Vin);

public sealed record UpdateVehicleCommand(
    Guid VehicleId,
    string Make,
    string Model,
    int Year,
    string? Color,
    string? Vin);

public sealed record VehicleDto(
    Guid Id,
    Guid CustomerId,
    string LicensePlate,
    string Make,
    string Model,
    int Year,
    string? Color,
    string? Vin,
    DateTimeOffset CreatedAt);
