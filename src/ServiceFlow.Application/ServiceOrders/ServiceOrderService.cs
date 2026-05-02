using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.ServiceOrders;

internal sealed class ServiceOrderService : IServiceOrderService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IValidator<OpenServiceOrderCommand> _openValidator;
    private readonly IValidator<TransitionServiceOrderCommand> _transitionValidator;
    private readonly IValidator<AssignStaffCommand> _assignValidator;
    private readonly IValidator<ServiceOrderListFilter> _listValidator;

    public ServiceOrderService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IValidator<OpenServiceOrderCommand> openValidator,
        IValidator<TransitionServiceOrderCommand> transitionValidator,
        IValidator<AssignStaffCommand> assignValidator,
        IValidator<ServiceOrderListFilter> listValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _openValidator = openValidator;
        _transitionValidator = transitionValidator;
        _assignValidator = assignValidator;
        _listValidator = listValidator;
    }

    public async Task<ServiceOrderDetailDto> OpenAsync(OpenServiceOrderCommand command, CancellationToken cancellationToken = default)
    {
        await _openValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        var currentUserId = _currentUser.RequireUserId();

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == command.VehicleId, cancellationToken)
            ?? throw new NotFoundException("Vehicle", command.VehicleId);
        if (vehicle.CustomerId != command.CustomerId)
        {
            throw new ConflictException("service_order.vehicle_customer_mismatch", "Vehicle does not belong to the specified customer.");
        }

        if (command.AssignedStaffId is { } staffId)
        {
            var staffOk = await _db.Users.AnyAsync(
                u => u.Id == staffId && (u.Role == UserRole.Staff || u.Role == UserRole.Admin) && u.IsActive,
                cancellationToken);
            if (!staffOk)
            {
                throw new NotFoundException("Staff", staffId);
            }
        }

        var order = ServiceOrder.Open(
            command.CustomerId,
            command.VehicleId,
            command.OpeningDescription,
            currentUserId,
            command.AssignedStaffId,
            _clock.UtcNow);

        _db.ServiceOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(order.Id, cancellationToken))!;
    }

    public async Task<ServiceOrderDetailDto> TransitionAsync(TransitionServiceOrderCommand command, CancellationToken cancellationToken = default)
    {
        await _transitionValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        var currentUserId = _currentUser.RequireUserId();

        var order = await _db.ServiceOrders
            .Include(o => o.RepairRequests)
            .FirstOrDefaultAsync(o => o.Id == command.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException("ServiceOrder", command.ServiceOrderId);

        order.TransitionTo(command.NextStage, currentUserId, command.Note, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(order.Id, cancellationToken))!;
    }

    public async Task<ServiceOrderDetailDto> AssignStaffAsync(AssignStaffCommand command, CancellationToken cancellationToken = default)
    {
        await _assignValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var staffOk = await _db.Users.AnyAsync(
            u => u.Id == command.StaffUserId && (u.Role == UserRole.Staff || u.Role == UserRole.Admin) && u.IsActive,
            cancellationToken);
        if (!staffOk)
        {
            throw new NotFoundException("Staff", command.StaffUserId);
        }

        var order = await _db.ServiceOrders.FirstOrDefaultAsync(o => o.Id == command.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException("ServiceOrder", command.ServiceOrderId);

        order.AssignStaff(command.StaffUserId);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(order.Id, cancellationToken))!;
    }

    public async Task<ServiceOrderDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _db.ServiceOrders
            .AsNoTracking()
            .Include(o => o.StatusHistory)
            .Include(o => o.RepairRequests)
                .ThenInclude(r => r.Media)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            return null;
        }

        EnsureCustomerAccess(order.CustomerId);

        var customer = await _db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, cancellationToken);
        var vehicle = await _db.Vehicles.AsNoTracking().FirstAsync(v => v.Id == order.VehicleId, cancellationToken);
        var staff = order.AssignedStaffId is { } staffId
            ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == staffId, cancellationToken)
            : null;

        return ServiceOrderMapper.ToDetail(order, customer, vehicle, staff, order.StatusHistory, order.RepairRequests);
    }

    public async Task<PagedResult<ServiceOrderSummaryDto>> ListAsync(ServiceOrderListFilter filter, CancellationToken cancellationToken = default)
    {
        await _listValidator.ValidateAndThrowAppAsync(filter, cancellationToken);

        var query = _db.ServiceOrders.AsNoTracking().AsQueryable();

        if (_currentUser.IsCustomer)
        {
            var customerId = await _db.Customers
                .AsNoTracking()
                .Where(c => c.UserId == _currentUser.UserId)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (customerId is null)
            {
                return new PagedResult<ServiceOrderSummaryDto>([], filter.Page, filter.PageSize, 0);
            }
            query = query.Where(o => o.CustomerId == customerId);
        }
        else
        {
            if (filter.CustomerId is { } cId) query = query.Where(o => o.CustomerId == cId);
            if (filter.AssignedStaffId is { } sId) query = query.Where(o => o.AssignedStaffId == sId);
        }

        if (filter.Stage is { } stage) query = query.Where(o => o.Stage == stage);

        var joined = query
            .Join(_db.Customers, o => o.CustomerId, c => c.Id, (o, c) => new { o, c })
            .Join(_db.Vehicles, oc => oc.o.VehicleId, v => v.Id, (oc, v) => new { oc.o, oc.c, v });

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            joined = joined.Where(x =>
                x.c.FullName.ToLower().Contains(s) ||
                x.v.LicensePlate.ToLower().Contains(s) ||
                x.v.Make.ToLower().Contains(s) ||
                x.v.Model.ToLower().Contains(s));
        }

        var total = await joined.CountAsync(cancellationToken);
        var page = await joined
            .OrderByDescending(x => x.o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new ServiceOrderSummaryDto(
                x.o.Id,
                x.c.Id,
                x.c.FullName,
                x.v.Id,
                x.v.Year + " " + x.v.Make + " " + x.v.Model,
                x.o.AssignedStaffId,
                x.o.Stage,
                x.o.CreatedAt,
                x.o.UpdatedAt,
                x.o.CompletedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceOrderSummaryDto>(page, filter.Page, filter.PageSize, total);
    }

    private void EnsureCustomerAccess(Guid customerId)
    {
        if (!_currentUser.IsCustomer)
        {
            return;
        }

        var owningUserId = _db.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => c.UserId)
            .FirstOrDefault();

        if (owningUserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You do not have access to this order.");
        }
    }
}
