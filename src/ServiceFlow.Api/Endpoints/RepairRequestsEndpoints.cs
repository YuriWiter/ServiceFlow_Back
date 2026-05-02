using ServiceFlow.Api.Authorization;
using ServiceFlow.Application.RepairRequests;

namespace ServiceFlow.Api.Endpoints;

internal static class RepairRequestsEndpoints
{
    public static IEndpointRouteBuilder MapRepairRequestsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/repair-requests").WithTags("RepairRequests")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapGet("/{id:guid}", async (Guid id, IRepairRequestService service, CancellationToken ct) =>
        {
            var repair = await service.GetAsync(id, ct);
            return repair is null ? Results.NotFound() : Results.Ok(repair);
        });

        group.MapGet("/by-service-order/{serviceOrderId:guid}", async (Guid serviceOrderId, IRepairRequestService service, CancellationToken ct) =>
            Results.Ok(await service.ListByServiceOrderAsync(serviceOrderId, ct)));

        group.MapPost("/", async (CreateRepairRequestCommand command, IRepairRequestService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(command, ct);
            return Results.Created($"/api/v1/repair-requests/{created.Id}", created);
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOnly)
        .WithSummary("Staff: request customer approval for a repair");

        group.MapPut("/{id:guid}/estimate", async (
            Guid id,
            UpdateEstimatePayload payload,
            IRepairRequestService service,
            CancellationToken ct) =>
        {
            var cmd = new UpdateRepairEstimateCommand(id, payload.IssueDescription, payload.PriceEstimateCents, payload.Urgency);
            return Results.Ok(await service.UpdateEstimateAsync(cmd, ct));
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOnly)
        .WithSummary("Staff: revise an estimate before the customer decides");

        group.MapPost("/{id:guid}/media", async (
            Guid id,
            AttachMediaPayload payload,
            IRepairRequestService service,
            CancellationToken ct) =>
        {
            var cmd = new AttachRepairMediaCommand(id, payload.MediaType, payload.StoragePath, payload.MimeType, payload.SizeBytes);
            return Results.Ok(await service.AttachMediaAsync(cmd, ct));
        })
        .RequireAuthorization(AuthorizationPolicies.StaffOnly)
        .WithSummary("Staff: attach proof (photo/video) to a repair request");

        group.MapPost("/{id:guid}/decision", async (
            Guid id,
            DecisionPayload payload,
            IRepairRequestService service,
            CancellationToken ct) =>
        {
            var cmd = new RegisterCustomerDecisionCommand(id, payload.Decision, payload.Note);
            return Results.Ok(await service.RegisterCustomerDecisionAsync(cmd, ct));
        }).WithSummary("Customer (or staff on their behalf): approve or reject a repair");

        return app;
    }

    internal sealed record UpdateEstimatePayload(
        string IssueDescription,
        long PriceEstimateCents,
        Domain.RepairRequests.RepairUrgency Urgency);

    internal sealed record AttachMediaPayload(
        Domain.RepairRequests.MediaType MediaType,
        string StoragePath,
        string? MimeType,
        long SizeBytes);

    internal sealed record DecisionPayload(
        Domain.RepairRequests.RepairDecision Decision,
        string? Note);
}
