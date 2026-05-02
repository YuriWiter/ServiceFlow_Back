using ServiceFlow.Application.Common;

namespace ServiceFlow.Application.ServiceOrders;

public interface IServiceOrderService
{
    Task<ServiceOrderDetailDto> OpenAsync(OpenServiceOrderCommand command, CancellationToken cancellationToken = default);
    Task<ServiceOrderDetailDto> TransitionAsync(TransitionServiceOrderCommand command, CancellationToken cancellationToken = default);
    Task<ServiceOrderDetailDto> AssignStaffAsync(AssignStaffCommand command, CancellationToken cancellationToken = default);
    Task<ServiceOrderDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ServiceOrderSummaryDto>> ListAsync(ServiceOrderListFilter filter, CancellationToken cancellationToken = default);
}
