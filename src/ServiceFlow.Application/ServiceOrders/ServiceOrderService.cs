using FluentValidation;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.ServiceOrders;

internal sealed class ServiceOrderService : IServiceOrderService
{
    private readonly IServiceFlowPersistence _persistence;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IFirestoreSyncService _firestoreSync;
    private readonly IValidator<OpenServiceOrderCommand> _openValidator;
    private readonly IValidator<TransitionServiceOrderCommand> _transitionValidator;
    private readonly IValidator<AssignStaffCommand> _assignValidator;
    private readonly IValidator<ServiceOrderListFilter> _listValidator;

    public ServiceOrderService(
        IServiceFlowPersistence persistence,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IFirestoreSyncService firestoreSync,
        IValidator<OpenServiceOrderCommand> openValidator,
        IValidator<TransitionServiceOrderCommand> transitionValidator,
        IValidator<AssignStaffCommand> assignValidator,
        IValidator<ServiceOrderListFilter> listValidator)
    {
        _persistence = persistence;
        _currentUser = currentUser;
        _clock = clock;
        _firestoreSync = firestoreSync;
        _openValidator = openValidator;
        _transitionValidator = transitionValidator;
        _assignValidator = assignValidator;
        _listValidator = listValidator;
    }

    public async Task<ServiceOrderDetailDto> OpenAsync(OpenServiceOrderCommand command, CancellationToken cancellationToken = default)
    {
        await _openValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        var currentUserId = _currentUser.RequireUserId();

        var vehicle = await _persistence.FindVehicleByIdAsync(command.VehicleId, cancellationToken)
            ?? throw new NotFoundException("Vehicle", command.VehicleId);
        if (vehicle.CustomerId != command.CustomerId)
        {
            throw new ConflictException("service_order.vehicle_customer_mismatch", "Vehicle does not belong to the specified customer.");
        }

        if (command.AssignedStaffId is { } staffId)
        {
            var staffOk = await _persistence.AnyActiveStaffOrAdminAsync(staffId, cancellationToken);
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

        await _persistence.AddServiceOrderAsync(order, cancellationToken);
        await _persistence.SaveChangesAsync(cancellationToken);

        await SyncServiceOrderToFirestoreAsync(order.Id, cancellationToken);

        return (await GetAsync(order.Id, cancellationToken))!;
    }

    public async Task<ServiceOrderDetailDto> TransitionAsync(TransitionServiceOrderCommand command, CancellationToken cancellationToken = default)
    {
        await _transitionValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        var currentUserId = _currentUser.RequireUserId();

        var order = await _persistence.FindServiceOrderForTransitionAsync(command.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException("ServiceOrder", command.ServiceOrderId);

        order.TransitionTo(command.NextStage, currentUserId, command.Note, _clock.UtcNow);
        await _persistence.PersistServiceOrderAsync(order, cancellationToken);
        await SyncServiceOrderToFirestoreAsync(order.Id, cancellationToken);

        return (await GetAsync(order.Id, cancellationToken))!;
    }

    public async Task<ServiceOrderDetailDto> AssignStaffAsync(AssignStaffCommand command, CancellationToken cancellationToken = default)
    {
        await _assignValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var staffOk = await _persistence.AnyActiveStaffOrAdminAsync(command.StaffUserId, cancellationToken);
        if (!staffOk)
        {
            throw new NotFoundException("Staff", command.StaffUserId);
        }

        var order = await _persistence.FindServiceOrderForAssignAsync(command.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException("ServiceOrder", command.ServiceOrderId);

        order.AssignStaff(command.StaffUserId);
        await _persistence.PersistServiceOrderAsync(order, cancellationToken);
        await SyncServiceOrderToFirestoreAsync(order.Id, cancellationToken);

        return (await GetAsync(order.Id, cancellationToken))!;
    }

    public async Task<ServiceOrderDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bundle = await _persistence.GetServiceOrderDetailBundleAsync(id, cancellationToken);
        if (bundle is null)
        {
            return null;
        }

        var (order, customer, vehicle, staff) = bundle.Value;
        await EnsureCustomerAccessAsync(order.CustomerId, cancellationToken);

        return ServiceOrderMapper.ToDetail(order, customer, vehicle, staff, order.StatusHistory, order.RepairRequests);
    }

    public async Task<PagedResult<ServiceOrderSummaryDto>> ListAsync(ServiceOrderListFilter filter, CancellationToken cancellationToken = default)
    {
        await _listValidator.ValidateAndThrowAppAsync(filter, cancellationToken);

        Guid? restrictToCustomerId = null;
        if (_currentUser.IsCustomer)
        {
            restrictToCustomerId = await _persistence.FindCustomerIdByUserIdAsync(_currentUser.UserId!.Value, cancellationToken);
            if (restrictToCustomerId is null)
            {
                return new PagedResult<ServiceOrderSummaryDto>([], filter.Page, filter.PageSize, 0);
            }
        }

        var (rows, total) = await _persistence.ListServiceOrdersAsync(filter, restrictToCustomerId, cancellationToken);
        var dtos = rows.Select(x => new ServiceOrderSummaryDto(
            x.Order.Id,
            x.Customer.Id,
            x.Customer.FullName,
            x.Vehicle.Id,
            ServiceOrderMapper.FormatVehicle(x.Vehicle),
            x.Order.AssignedStaffId,
            x.Order.Stage,
            x.Order.CreatedAt,
            x.Order.UpdatedAt,
            x.Order.CompletedAt)).ToList();

        return new PagedResult<ServiceOrderSummaryDto>(dtos, filter.Page, filter.PageSize, total);
    }

    private async Task EnsureCustomerAccessAsync(Guid customerId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsCustomer)
        {
            return;
        }

        var owningUserId = await _persistence.FindUserIdByCustomerIdAsync(customerId, cancellationToken);
        if (owningUserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You do not have access to this order.");
        }
    }

    private async Task SyncServiceOrderToFirestoreAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        var snapshot = await _persistence.FindServiceOrderWithHistoryAndRepairsReadOnlyAsync(serviceOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Service order {serviceOrderId} not found after save.");
        await _firestoreSync.UpsertServiceOrderAsync(snapshot, cancellationToken);
    }
}
