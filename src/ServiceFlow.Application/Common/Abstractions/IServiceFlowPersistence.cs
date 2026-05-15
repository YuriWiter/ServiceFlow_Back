using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;
using ServiceFlow.Application.ServiceOrders;

namespace ServiceFlow.Application.Common.Abstractions;

public interface IServiceFlowPersistence
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> UserExistsWithEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<User?> FindUserByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AnyActiveStaffOrAdminAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<Customer?> FindCustomerByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> FindCustomerByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Customer> Items, int Total)> SearchCustomersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Guid?> FindCustomerIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid?> FindUserIdByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<bool> CustomerExistsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> VehicleLicensePlateExistsAsync(string normalizedPlate, CancellationToken cancellationToken = default);
    Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task<Vehicle?> FindVehicleByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Vehicle?> FindVehicleByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> ListVehiclesByCustomerReadOnlyAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Vehicle?> FindVehicleByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default);
    Task<ServiceOrder?> FindServiceOrderForTransitionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceOrder?> FindServiceOrderForAssignAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(ServiceOrder Order, Customer Customer, Vehicle Vehicle, User? Staff)?>
        GetServiceOrderDetailBundleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<(ServiceOrder Order, Customer Customer, Vehicle Vehicle)> Rows, int Total)> ListServiceOrdersAsync(
        ServiceOrderListFilter filter,
        Guid? restrictToCustomerId,
        CancellationToken cancellationToken = default);

    Task<RepairRequest?> FindRepairRequestTrackedWithMediaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RepairRequest?> FindRepairRequestReadOnlyWithMediaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepairRequest>> ListRepairRequestsByServiceOrderReadOnlyAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken = default);

    Task<ServiceOrder?> FindServiceOrderWithHistoryAndRepairsReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task PersistServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default);
    Task PersistRepairRequestAsync(RepairRequest repair, CancellationToken cancellationToken = default);
    Task PersistCustomerAsync(Customer customer, CancellationToken cancellationToken = default);
    Task PersistVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task PersistNewRepairOnOrderAsync(ServiceOrder order, RepairRequest repair, CancellationToken cancellationToken = default);
    Task<Guid?> FindPortalUserIdForServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken = default);
}
