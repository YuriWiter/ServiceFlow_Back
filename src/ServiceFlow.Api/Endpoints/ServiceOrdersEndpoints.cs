using ServiceFlow.Api.Authorization;
using ServiceFlow.Application.ServiceOrders;
using ServiceFlow.Domain.ServiceOrders;

namespace ServiceFlow.Api.Endpoints;

internal static class ServiceOrdersEndpoints
{
    public static IEndpointRouteBuilder MapServiceOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/service-orders").WithTags("ServiceOrders")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapGet("/", async (
            ServiceStage? stage,
            Guid? customerId,
            Guid? staffId,
            string? search,
            int? page,
            int? pageSize,
            IServiceOrderService service,
            CancellationToken ct) =>
        {
            var filter = new ServiceOrderListFilter(stage, customerId, staffId, search, page ?? 1, pageSize ?? 20);
            return Results.Ok(await service.ListAsync(filter, ct));
        }).WithSummary("List service orders (customers see only theirs)");

        group.MapGet("/{id:guid}", async (Guid id, IServiceOrderService service, CancellationToken ct) =>
        {
            var order = await service.GetAsync(id, ct);
            return order is null ? Results.NotFound() : Results.Ok(order);
        }).WithSummary("Fetch a single service order with history + repairs");

        group.MapPost("/", async (OpenServiceOrderCommand command, IServiceOrderService service, CancellationToken ct) =>
        {
            var created = await service.OpenAsync(command, ct);
            return Results.Created($"/api/v1/service-orders/{created.Id}", created);
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOnly)
        .WithSummary("Open a new service order for a vehicle");

        group.MapPost("/{id:guid}/transition", async (
            Guid id,
            TransitionPayload payload,
            IServiceOrderService service,
            CancellationToken ct) =>
        {
            var cmd = new TransitionServiceOrderCommand(id, payload.Stage, payload.Note);
            return Results.Ok(await service.TransitionAsync(cmd, ct));
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOnly)
        .WithSummary("Advance a service order to the next allowed stage");

        group.MapPost("/{id:guid}/assign", async (
            Guid id,
            AssignPayload payload,
            IServiceOrderService service,
            CancellationToken ct) =>
        {
            var cmd = new AssignStaffCommand(id, payload.StaffUserId);
            return Results.Ok(await service.AssignStaffAsync(cmd, ct));
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOnly)
        .WithSummary("Assign a staff member to the order");

        return app;
    }

    internal sealed record TransitionPayload(ServiceStage Stage, string? Note);
    internal sealed record AssignPayload(Guid StaffUserId);
}
