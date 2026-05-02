using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.ServiceOrders;

namespace ServiceFlow.Application.RepairRequests;

internal sealed class RepairRequestService : IRepairRequestService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IValidator<CreateRepairRequestCommand> _createValidator;
    private readonly IValidator<UpdateRepairEstimateCommand> _updateValidator;
    private readonly IValidator<AttachRepairMediaCommand> _attachValidator;
    private readonly IValidator<RegisterCustomerDecisionCommand> _decisionValidator;

    public RepairRequestService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IValidator<CreateRepairRequestCommand> createValidator,
        IValidator<UpdateRepairEstimateCommand> updateValidator,
        IValidator<AttachRepairMediaCommand> attachValidator,
        IValidator<RegisterCustomerDecisionCommand> decisionValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
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

        var order = await _db.ServiceOrders
            .Include(o => o.RepairRequests)
            .FirstOrDefaultAsync(o => o.Id == command.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException("ServiceOrder", command.ServiceOrderId);

        var repair = order.AddRepairRequest(
            userId,
            command.IssueDescription,
            command.PriceEstimateCents,
            command.Urgency);

        await _db.SaveChangesAsync(cancellationToken);
        return RepairRequestMapper.ToDto(repair);
    }

    public async Task<RepairRequestDto> UpdateEstimateAsync(UpdateRepairEstimateCommand command, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        if (!_currentUser.IsStaff)
        {
            throw new ForbiddenException("Only staff can update repair estimates.");
        }

        var repair = await _db.RepairRequests
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == command.RepairRequestId, cancellationToken)
            ?? throw new NotFoundException("RepairRequest", command.RepairRequestId);

        repair.UpdateEstimate(command.IssueDescription, command.PriceEstimateCents, command.Urgency);
        await _db.SaveChangesAsync(cancellationToken);
        return RepairRequestMapper.ToDto(repair);
    }

    public async Task<RepairRequestDto> AttachMediaAsync(AttachRepairMediaCommand command, CancellationToken cancellationToken = default)
    {
        await _attachValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        if (!_currentUser.IsStaff)
        {
            throw new ForbiddenException("Only staff can attach repair media.");
        }

        var repair = await _db.RepairRequests
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == command.RepairRequestId, cancellationToken)
            ?? throw new NotFoundException("RepairRequest", command.RepairRequestId);

        repair.AttachMedia(command.MediaType, command.StoragePath, command.MimeType, command.SizeBytes);
        await _db.SaveChangesAsync(cancellationToken);
        return RepairRequestMapper.ToDto(repair);
    }

    public async Task<RepairRequestDto> RegisterCustomerDecisionAsync(RegisterCustomerDecisionCommand command, CancellationToken cancellationToken = default)
    {
        await _decisionValidator.ValidateAndThrowAppAsync(command, cancellationToken);
        _currentUser.RequireUserId();

        var repair = await _db.RepairRequests
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == command.RepairRequestId, cancellationToken)
            ?? throw new NotFoundException("RepairRequest", command.RepairRequestId);

        await EnsureDecisionAuthorityAsync(repair.ServiceOrderId, cancellationToken);

        repair.RegisterDecision(command.Decision, command.Note, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return RepairRequestMapper.ToDto(repair);
    }

    public async Task<RepairRequestDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var repair = await _db.RepairRequests
            .AsNoTracking()
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return repair is null ? null : RepairRequestMapper.ToDto(repair);
    }

    public async Task<IReadOnlyList<RepairRequestDto>> ListByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken = default)
    {
        var repairs = await _db.RepairRequests
            .AsNoTracking()
            .Include(r => r.Media)
            .Where(r => r.ServiceOrderId == serviceOrderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return repairs.Select(RepairRequestMapper.ToDto).ToList();
    }

    private async Task EnsureDecisionAuthorityAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsStaff)
        {
            return;
        }

        var owningUserId = await _db.ServiceOrders
            .AsNoTracking()
            .Where(o => o.Id == serviceOrderId)
            .Join(_db.Customers, o => o.CustomerId, c => c.Id, (o, c) => c.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (owningUserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You do not own this repair request.");
        }
    }
}
