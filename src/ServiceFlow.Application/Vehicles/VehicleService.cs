using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Application.Vehicles;

internal sealed class VehicleService : IVehicleService
{
    private readonly IAppDbContext _db;
    private readonly IValidator<CreateVehicleCommand> _createValidator;
    private readonly IValidator<UpdateVehicleCommand> _updateValidator;

    public VehicleService(
        IAppDbContext db,
        IValidator<CreateVehicleCommand> createValidator,
        IValidator<UpdateVehicleCommand> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var customerExists = await _db.Customers.AnyAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new NotFoundException("Customer", command.CustomerId);
        }

        var plate = command.LicensePlate.Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
        var duplicate = await _db.Vehicles.AnyAsync(v => v.LicensePlate == plate, cancellationToken);
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

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(vehicle);
    }

    public async Task<VehicleDto> UpdateAsync(UpdateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == command.VehicleId, cancellationToken)
            ?? throw new NotFoundException("Vehicle", command.VehicleId);

        vehicle.UpdateDetails(command.Make, command.Model, command.Year, command.Color, command.Vin);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(vehicle);
    }

    public async Task<VehicleDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        return vehicle is null ? null : Map(vehicle);
    }

    public async Task<IReadOnlyList<VehicleDto>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => Map(v))
            .ToListAsync(cancellationToken);
    }

    private static VehicleDto Map(Vehicle v)
        => new(v.Id, v.CustomerId, v.LicensePlate, v.Make, v.Model, v.Year, v.Color, v.Vin, v.CreatedAt);
}
