using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.ServiceOrders;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Infrastructure.Persistence;

public sealed class EfServiceFlowPersistence(IAppDbContext db) : IServiceFlowPersistence
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task<bool> UserExistsWithEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        db.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> FindUserByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<User?> FindUserByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> AnyActiveStaffOrAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.AnyAsync(
            u => u.Id == userId && (u.Role == UserRole.Staff || u.Role == UserRole.Admin) && u.IsActive,
            cancellationToken);

    public Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        db.Customers.Add(customer);
        return Task.CompletedTask;
    }

    public Task<Customer?> FindCustomerByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> FindCustomerByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Customer> Items, int Total)> SearchCustomersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                c.PhoneNumber.Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Guid?> FindCustomerIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Customers.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Guid?> FindUserIdByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => c.UserId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> CustomerExistsAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        db.Customers.AnyAsync(c => c.Id == customerId, cancellationToken);

    public Task<bool> VehicleLicensePlateExistsAsync(string normalizedPlate, CancellationToken cancellationToken = default) =>
        db.Vehicles.AnyAsync(v => v.LicensePlate == normalizedPlate, cancellationToken);

    public Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        db.Vehicles.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task<Vehicle?> FindVehicleByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Vehicle?> FindVehicleByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Vehicle?> FindVehicleByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        FindVehicleByIdTrackedAsync(id, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> ListVehiclesByCustomerReadOnlyAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await db.Vehicles.AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default)
    {
        db.ServiceOrders.Add(order);
        return Task.CompletedTask;
    }

    public Task<ServiceOrder?> FindServiceOrderForTransitionAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ServiceOrders
            .Include(o => o.RepairRequests)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<ServiceOrder?> FindServiceOrderForAssignAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ServiceOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<(ServiceOrder Order, Customer Customer, Vehicle Vehicle, User? Staff)?>
        GetServiceOrderDetailBundleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await db.ServiceOrders
            .AsNoTracking()
            .Include(o => o.StatusHistory)
            .Include(o => o.RepairRequests)
            .ThenInclude(r => r.Media)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, cancellationToken);
        var vehicle = await db.Vehicles.AsNoTracking().FirstAsync(v => v.Id == order.VehicleId, cancellationToken);
        var staff = order.AssignedStaffId is { } sid
            ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == sid, cancellationToken)
            : null;

        return (order, customer, vehicle, staff);
    }

    public async Task<(IReadOnlyList<(ServiceOrder Order, Customer Customer, Vehicle Vehicle)> Rows, int Total)>
        ListServiceOrdersAsync(
            ServiceOrderListFilter filter,
            Guid? restrictToCustomerId,
            CancellationToken cancellationToken = default)
    {
        var query = db.ServiceOrders.AsNoTracking().AsQueryable();

        if (restrictToCustomerId is { } cid)
        {
            query = query.Where(o => o.CustomerId == cid);
        }
        else
        {
            if (filter.CustomerId is { } fc) query = query.Where(o => o.CustomerId == fc);
            if (filter.AssignedStaffId is { } fs) query = query.Where(o => o.AssignedStaffId == fs);
        }

        if (filter.Stage is { } stage) query = query.Where(o => o.Stage == stage);

        var joined = query
            .Join(db.Customers, o => o.CustomerId, c => c.Id, (o, c) => new { o, c })
            .Join(db.Vehicles, oc => oc.o.VehicleId, v => v.Id, (oc, v) => new { oc.o, oc.c, v });

        var joinedQuery = joined;
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            joinedQuery = joined.Where(x =>
                x.c.FullName.ToLower().Contains(s) ||
                x.v.LicensePlate.ToLower().Contains(s) ||
                x.v.Make.ToLower().Contains(s) ||
                x.v.Model.ToLower().Contains(s));
        }

        var total = await joinedQuery.CountAsync(cancellationToken);
        var raw = await joinedQuery
            .OrderByDescending(x => x.o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new { x.o, x.c, x.v })
            .ToListAsync(cancellationToken);
        var page = raw.ConvertAll(x => (x.o, x.c, x.v));

        return (page, total);
    }

    public Task<RepairRequest?> FindRepairRequestTrackedWithMediaAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.RepairRequests
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<RepairRequest?> FindRepairRequestReadOnlyWithMediaAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.RepairRequests.AsNoTracking()
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RepairRequest>> ListRepairRequestsByServiceOrderReadOnlyAsync(
        Guid serviceOrderId,
        CancellationToken cancellationToken = default) =>
        await db.RepairRequests.AsNoTracking()
            .Include(r => r.Media)
            .Where(r => r.ServiceOrderId == serviceOrderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<ServiceOrder?> FindServiceOrderWithHistoryAndRepairsReadOnlyAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        db.ServiceOrders.AsNoTracking()
            .Include(o => o.StatusHistory)
            .Include(o => o.RepairRequests)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task PersistServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task PersistRepairRequestAsync(RepairRequest repair, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task PersistCustomerAsync(Customer customer, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task PersistVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task PersistNewRepairOnOrderAsync(ServiceOrder order, RepairRequest repair, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public Task<Guid?> FindPortalUserIdForServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken = default) =>
        db.ServiceOrders.AsNoTracking()
            .Where(o => o.Id == serviceOrderId)
            .Join(db.Customers, o => o.CustomerId, c => c.Id, (o, c) => c.UserId)
            .FirstOrDefaultAsync(cancellationToken);
}
