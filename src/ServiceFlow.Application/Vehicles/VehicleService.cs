using FluentValidation;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Application.Vehicles;

internal sealed class VehicleService : IVehicleService
{
    private readonly IServiceFlowPersistence _persistence;
    private readonly IFirestoreSyncService _firestoreSync;
    private readonly IValidator<CreateVehicleCommand> _createValidator;
    private readonly IValidator<UpdateVehicleCommand> _updateValidator;

    public VehicleService(
        IServiceFlowPersistence persistence,
        IFirestoreSyncService firestoreSync,
        IValidator<CreateVehicleCommand> createValidator,
        IValidator<UpdateVehicleCommand> updateValidator)
    {
        _persistence = persistence;
        _firestoreSync = firestoreSync;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var customerExists = await _persistence.CustomerExistsAsync(command.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new NotFoundException("Customer", command.CustomerId);
        }

        var plate = command.LicensePlate.Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
        var duplicate = await _persistence.VehicleLicensePlateExistsAsync(plate, cancellationToken);
        if (duplicate)
        {
            throw new ConflictException("vehicle.license_plate.taken", "License plate is already registered.");
        }

        var vehicle = Vehicle.Create(
            command.CustomerId,
            command.LicensePlate,
            command.Make,
            command.Model,
            command.Year,
            command.Color,
            command.Vin);

        await _persistence.AddVehicleAsync(vehicle, cancellationToken);
        await _persistence.SaveChangesAsync(cancellationToken);
        await _firestoreSync.UpsertVehicleAsync(vehicle, cancellationToken);
        return Map(vehicle);
    }

    public async Task<VehicleDto> UpdateAsync(UpdateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var vehicle = await _persistence.FindVehicleByIdTrackedAsync(command.VehicleId, cancellationToken)
            ?? throw new NotFoundException("Vehicle", command.VehicleId);

        vehicle.UpdateDetails(command.Make, command.Model, command.Year, command.Color, command.Vin);
        await _persistence.PersistVehicleAsync(vehicle, cancellationToken);
        await _firestoreSync.UpsertVehicleAsync(vehicle, cancellationToken);
        return Map(vehicle);
    }

    public async Task<VehicleDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _persistence.FindVehicleByIdReadOnlyAsync(id, cancellationToken);
        return vehicle is null ? null : Map(vehicle);
    }

    public async Task<IReadOnlyList<VehicleDto>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var list = await _persistence.ListVehiclesByCustomerReadOnlyAsync(customerId, cancellationToken);
        return list.Select(Map).ToList();
    }

    private static VehicleDto Map(Vehicle v)
        => new(v.Id, v.CustomerId, v.LicensePlate, v.Make, v.Model, v.Year, v.Color, v.Vin, v.CreatedAt);
}
