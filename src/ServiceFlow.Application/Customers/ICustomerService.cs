using ServiceFlow.Application.Common;

namespace ServiceFlow.Application.Customers;

public interface ICustomerService
{
    Task<CustomerDto> CreateAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<CustomerDto>> ListAsync(CustomerListFilter filter, CancellationToken cancellationToken = default);
}
