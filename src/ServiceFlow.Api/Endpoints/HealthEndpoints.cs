namespace ServiceFlow.Api.Endpoints;

internal static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/health").WithTags("Health").AllowAnonymous();

        group.MapGet("/live", () => Results.Ok(new { status = "alive" }))
            .WithName("HealthLive")
            .WithSummary("Liveness probe")
            .WithDescription("Returns 200 if the process is up. Does not touch external dependencies.");

        group.MapGet("/ready", () => Results.Ok(new { status = "ready" }))
            .WithName("HealthReady")
            .WithSummary("Readiness probe");

        return app;
    }
}
