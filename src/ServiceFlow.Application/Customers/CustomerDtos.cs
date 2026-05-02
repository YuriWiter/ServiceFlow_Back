namespace ServiceFlow.Application.Customers;

public sealed record CreateCustomerCommand(string FullName, string PhoneNumber, string? Email);

public sealed record UpdateCustomerCommand(Guid CustomerId, string FullName, string PhoneNumber, string? Email);

public sealed record CustomerDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string? Email,
    Guid? UserId,
    DateTimeOffset CreatedAt);

public sealed record CustomerListFilter(string? Search, int Page = 1, int PageSize = 20);
