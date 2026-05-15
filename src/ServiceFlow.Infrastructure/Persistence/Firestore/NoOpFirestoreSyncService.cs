using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Infrastructure.Persistence.Firestore;

internal sealed class NoOpFirestoreSyncService : IFirestoreSyncService
{
    public Task UpsertUserAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpsertCustomerAsync(Customer customer, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpsertVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpsertServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpsertRepairRequestAsync(RepairRequest repair, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
