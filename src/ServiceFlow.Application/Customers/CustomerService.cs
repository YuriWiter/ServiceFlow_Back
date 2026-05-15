using FluentValidation;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Customers;

namespace ServiceFlow.Application.Customers;

internal sealed class CustomerService : ICustomerService
{
    private readonly IServiceFlowPersistence _persistence;
    private readonly IFirestoreSyncService _firestoreSync;
    private readonly IValidator<CreateCustomerCommand> _createValidator;
    private readonly IValidator<UpdateCustomerCommand> _updateValidator;
    private readonly IValidator<CustomerListFilter> _listValidator;

    public CustomerService(
        IServiceFlowPersistence persistence,
        IFirestoreSyncService firestoreSync,
        IValidator<CreateCustomerCommand> createValidator,
        IValidator<UpdateCustomerCommand> updateValidator,
        IValidator<CustomerListFilter> listValidator)
    {
        _persistence = persistence;
        _firestoreSync = firestoreSync;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _listValidator = listValidator;
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var customer = Customer.Create(command.FullName, command.PhoneNumber, command.Email);
        await _persistence.AddCustomerAsync(customer, cancellationToken);
        await _persistence.SaveChangesAsync(cancellationToken);
        await _firestoreSync.UpsertCustomerAsync(customer, cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerDto> UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var customer = await _persistence.FindCustomerByIdTrackedAsync(command.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", command.CustomerId);

        customer.UpdateContact(command.FullName, command.PhoneNumber, command.Email);
        await _persistence.PersistCustomerAsync(customer, cancellationToken);
        await _firestoreSync.UpsertCustomerAsync(customer, cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _persistence.FindCustomerByIdReadOnlyAsync(id, cancellationToken);
        return customer is null ? null : Map(customer);
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(CustomerListFilter filter, CancellationToken cancellationToken = default)
    {
        await _listValidator.ValidateAndThrowAppAsync(filter, cancellationToken);

        var (items, total) = await _persistence.SearchCustomersAsync(filter.Search, filter.Page, filter.PageSize, cancellationToken);

        return new PagedResult<CustomerDto>(items.Select(Map).ToList(), filter.Page, filter.PageSize, total);
    }

    private static CustomerDto Map(Customer c)
        => new(c.Id, c.FullName, c.PhoneNumber, c.Email, c.UserId, c.CreatedAt);
}
