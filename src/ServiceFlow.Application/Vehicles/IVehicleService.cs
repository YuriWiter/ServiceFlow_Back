namespace ServiceFlow.Application.Vehicles;

public interface IVehicleService
{
    Task<VehicleDto> CreateAsync(CreateVehicleCommand command, CancellationToken cancellationToken = default);
    Task<VehicleDto> UpdateAsync(UpdateVehicleCommand command, CancellationToken cancellationToken = default);
    Task<VehicleDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehicleDto>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
