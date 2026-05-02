using ServiceFlow.Api.Authorization;
using ServiceFlow.Application.Customers;

namespace ServiceFlow.Api.Endpoints;

internal static class CustomersEndpoints
{
    public static IEndpointRouteBuilder MapCustomersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customers").WithTags("Customers")
            .RequireAuthorization(AuthorizationPolicies.StaffOnly);

        group.MapGet("/", async (
            string? search,
            int? page,
            int? pageSize,
            ICustomerService service,
            CancellationToken ct) =>
        {
            var filter = new CustomerListFilter(search, page ?? 1, pageSize ?? 20);
            var result = await service.ListAsync(filter, ct);
            return Results.Ok(result);
        }).WithSummary("List customers (paged, searchable)");

        group.MapGet("/{id:guid}", async (Guid id, ICustomerService service, CancellationToken ct) =>
        {
            var customer = await service.GetAsync(id, ct);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        }).WithSummary("Fetch a customer by id");

        group.MapPost("/", async (CreateCustomerCommand command, ICustomerService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(command, ct);
            return Results.Created($"/api/v1/customers/{created.Id}", created);
        }).WithSummary("Register a new customer (staff on behalf of a walk-in)");

        group.MapPut("/{id:guid}", async (Guid id, UpdateCustomerCommand command, ICustomerService service, CancellationToken ct) =>
        {
            if (id != command.CustomerId)
            {
                return Results.BadRequest(new { error = "Route id does not match body CustomerId." });
            }

            var updated = await service.UpdateAsync(command, ct);
            return Results.Ok(updated);
        }).WithSummary("Update a customer's profile");

        return app;
    }
}
