using ServiceFlow.Api.Authorization;
using ServiceFlow.Application.Vehicles;

namespace ServiceFlow.Api.Endpoints;

internal static class VehiclesEndpoints
{
    public static IEndpointRouteBuilder MapVehiclesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/vehicles").WithTags("Vehicles")
            .RequireAuthorization(AuthorizationPolicies.StaffOnly);

        group.MapGet("/{id:guid}", async (Guid id, IVehicleService service, CancellationToken ct) =>
        {
            var vehicle = await service.GetAsync(id, ct);
            return vehicle is null ? Results.NotFound() : Results.Ok(vehicle);
        });

        group.MapGet("/by-customer/{customerId:guid}", async (Guid customerId, IVehicleService service, CancellationToken ct) =>
            Results.Ok(await service.ListByCustomerAsync(customerId, ct)));

        group.MapPost("/", async (CreateVehicleCommand command, IVehicleService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(command, ct);
            return Results.Created($"/api/v1/vehicles/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateVehicleCommand command, IVehicleService service, CancellationToken ct) =>
        {
            if (id != command.VehicleId)
            {
                return Results.BadRequest(new { error = "Route id does not match body VehicleId." });
            }

            return Results.Ok(await service.UpdateAsync(command, ct));
        });

        return app;
    }
}
