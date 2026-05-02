namespace ServiceFlow.Application.RepairRequests;

public interface IRepairRequestService
{
    Task<RepairRequestDto> CreateAsync(CreateRepairRequestCommand command, CancellationToken cancellationToken = default);
    Task<RepairRequestDto> UpdateEstimateAsync(UpdateRepairEstimateCommand command, CancellationToken cancellationToken = default);
    Task<RepairRequestDto> AttachMediaAsync(AttachRepairMediaCommand command, CancellationToken cancellationToken = default);
    Task<RepairRequestDto> RegisterCustomerDecisionAsync(RegisterCustomerDecisionCommand command, CancellationToken cancellationToken = default);
    Task<RepairRequestDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepairRequestDto>> ListByServiceOrderAsync(Guid serviceOrderId, CancellationToken cancellationToken = default);
}
