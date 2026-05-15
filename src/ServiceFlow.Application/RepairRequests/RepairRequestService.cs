using FluentValidation;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.ServiceOrders;

namespace ServiceFlow.Application.RepairRequests;

internal sealed class RepairRequestService : IRepairRequestService
{
    private readonly IServiceFlowPersistence _persistence;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IFirestoreSyncService _firestoreSync;
    private readonly IValidator<CreateRepairRequestCommand> _createValidator;
    private readonly IValidator<UpdateRepairEstimateCommand> _updateValidator;
    private readonly IValidator<AttachRepairMediaCommand> _attachValidator;
    private readonly IValidator<RegisterCustomerDecisionCommand> _decisionValidator;

    public RepairRequestService(
        IServiceFlowPersistence persistence,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IFirestoreSyncService firestoreSync,
        IValidator<CreateRepairRequestCommand> createValidator,
        IValidator<UpdateRepairEstimateCommand> updateValidator,
        IValidator<AttachRepairMediaCommand> attachValidator,
        IValidator<RegisterCustomerDecisionCommand> decisionValidator)
    {
        _persistence = persistence;
        _currentUser = currentUser;
        _clock = clock;
        _firestoreSync = firestoreSync;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _attachValidator = attachValidator;
        _decisionValidator = decisionValidator;
    }

    public async Task<RepairRequestDto> CreateAsync(CreateRepairRequestCommand command, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        var userId = _currentUser.RequireUserId();
        if (!_currentUser.IsStaff)
        {
            throw new ForbiddenException("Only staff can create repair requests.");
        }

        var order = await _persistence.FindServiceOrderForTransitionAsync(command.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException("ServiceOrder", command.ServiceOrderId);

        var repair = order.AddRepairRequest(
            userId,
            command.IssueDescription,
            command.PriceEstimateCents,
            command.Urgency);

        await _persistence.PersistNewRepairOnOrderAsync(order, repair, cancellationToken);
        await SyncRepairAndParentOrderAsync(repair.Id, order.Id, cancellationToken);
        return RepairRequestMapper.ToDto(repair);
    }

    public async Task<RepairRequestDto> UpdateEstimateAsync(UpdateRepairEstimateCommand command, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        if (!_currentUser.IsStaff)
        {
            throw new ForbiddenException("Only staff can update repair estimates.");
        }

        var repair = await _persistence.FindRepairRequestTrackedWithMediaAsync(command.RepairRequestId, cancellationToken)
            ?? throw new NotFoundException("RepairRequest", command.RepairRequestId);

        repair.UpdateEstimate(command.IssueDescription, command.PriceEstimateCents, command.Urgency);
        await _persistence.PersistRepairRequestAsync(repair, cancellationToken);
        await SyncRepairAndParentOrderAsync(repair.Id, repair.ServiceOrderId, cancellationToken);
        return RepairRequestMapper.ToDto(repair);
    }

    public async Task<RepairRequestDto> AttachMediaAsync(AttachRepairMediaCommand command, CancellationToken cancellationToken = default)
    {
        await _attachValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        if (!_currentUser.IsStaff)
        {
            throw new ForbiddenException("Only staff can attach repair media.");
        }

        var repair = await _persistence.FindRepairRequestTrackedWithMediaAsync(command.RepairRequestId, cancellationToken)
            ?? throw new NotFoundException("RepairRequest", command.RepairRequestId);

        repair.AttachMedia(command.MediaType, command.StoragePath, command.MimeType, command.SizeBytes);
        await _persistence.PersistRepairRequestAsync(repair, cancellationToken);
        await SyncRepairAndParentOrderAsync(repair.Id, repair.ServiceOrderId, cancellationToken);
        return RepairRequestMapper.ToDto(repair);
    }

    public async Task<RepairRequestDto> RegisterCustomerDecisionAsync(RegisterCustomerDecisionCommand command, CancellationToken cancellationToken = default)
    {
        await _decisionValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        _currentUser.RequireUserId();

        var repair = await _persistence.FindRepairRequestTrackedWithMediaAsync(command.RepairRequestId, cancellationToken)
            ?? throw new NotFoundException("RepairRequest", command.RepairRequestId);

        await EnsureDecisionAuthorityAsync(repair.ServiceOrderId, cancellationToken);

        repair.RegisterDecision(command.Decision, command.Note, _clock.UtcNow);
        await _persistence.PersistRepairRequestAsync(repair, cancellationToken);
        await SyncRepairAndParentOrderAsync(repair.Id, repair.ServiceOrderId, cancellationToken);
        return RepairRequestMapper.ToDto(repair);
    }

    public async Task<RepairRequestDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var repair = await _persistence.FindRepairRequestReadOnlyWithMediaAsync(id, cancellationToken);
        return repair is null ? null : RepairRequestMapper.ToDto(repair);
    }

    public async Task<IReadOnlyList<RepairRequestDto>> ListByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken = default)
    {
        var repairs = await _persistence.ListRepairRequestsByServiceOrderReadOnlyAsync(serviceOrderId, cancellationToken);
        return repairs.Select(RepairRequestMapper.ToDto).ToList();
    }

    private async Task EnsureDecisionAuthorityAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsStaff)
        {
            return;
        }

        var owningUserId = await _persistence.FindPortalUserIdForServiceOrderAsync(serviceOrderId, cancellationToken);

        if (owningUserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You do not own this repair request.");
        }
    }

    private async Task SyncRepairAndParentOrderAsync(
        Guid repairRequestId,
        Guid serviceOrderId,
        CancellationToken cancellationToken)
    {
        var persisted = await _persistence.FindRepairRequestReadOnlyWithMediaAsync(repairRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Repair request {repairRequestId} not found after save.");
        await _firestoreSync.UpsertRepairRequestAsync(persisted, cancellationToken);

        var orderSnapshot = await _persistence.FindServiceOrderWithHistoryAndRepairsReadOnlyAsync(serviceOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Service order {serviceOrderId} not found.");
        await _firestoreSync.UpsertServiceOrderAsync(orderSnapshot, cancellationToken);
    }
}
