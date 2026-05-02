using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Customers;

namespace ServiceFlow.Application.Customers;

internal sealed class CustomerService : ICustomerService
{
    private readonly IAppDbContext _db;
    private readonly IValidator<CreateCustomerCommand> _createValidator;
    private readonly IValidator<UpdateCustomerCommand> _updateValidator;
    private readonly IValidator<CustomerListFilter> _listValidator;

    public CustomerService(
        IAppDbContext db,
        IValidator<CreateCustomerCommand> createValidator,
        IValidator<UpdateCustomerCommand> updateValidator,
        IValidator<CustomerListFilter> listValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _listValidator = listValidator;
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var customer = Customer.Create(command.FullName, command.PhoneNumber, command.Email);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerDto> UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAppAsync(command, cancellationToken);

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", command.CustomerId);

        customer.UpdateContact(command.FullName, command.PhoneNumber, command.Email);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    public async Task<CustomerDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return customer is null ? null : Map(customer);
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(CustomerListFilter filter, CancellationToken cancellationToken = default)
    {
        await _listValidator.ValidateAndThrowAppAsync(filter, cancellationToken);

        var query = _db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(search) ||
                (c.Email != null && c.Email.ToLower().Contains(search)) ||
                c.PhoneNumber.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => Map(c))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>(items, filter.Page, filter.PageSize, total);
    }

    private static CustomerDto Map(Customer c)
        => new(c.Id, c.FullName, c.PhoneNumber, c.Email, c.UserId, c.CreatedAt);
}
