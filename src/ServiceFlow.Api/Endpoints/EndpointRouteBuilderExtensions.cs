namespace ServiceFlow.Api.Endpoints;

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");
        api.MapHealthEndpoints();
        api.MapAuthEndpoints();
        api.MapCustomersEndpoints();
        api.MapVehiclesEndpoints();
        api.MapServiceOrdersEndpoints();
        api.MapRepairRequestsEndpoints();
        return app;
    }
}
