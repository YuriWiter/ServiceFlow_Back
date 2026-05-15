using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Application.Common.Abstractions;

/// <summary>
/// Best-effort projection of aggregates into Firestore after PostgreSQL commits. Disabled unless <c>Firestore:SyncWrites</c> is true.
/// </summary>
public interface IFirestoreSyncService
{
    Task UpsertUserAsync(User user, CancellationToken cancellationToken = default);

    Task UpsertCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    Task UpsertVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task UpsertServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default);

    Task UpsertRepairRequestAsync(RepairRequest repair, CancellationToken cancellationToken = default);
}
