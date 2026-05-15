using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ServiceFlow.Api.Endpoints;

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(
        this IEndpointRouteBuilder app,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        var api = app.MapGroup("/api/v1");
        api.MapHealthEndpoints();
        api.MapAuthEndpoints();
        api.MapCustomersEndpoints();
        api.MapVehiclesEndpoints();
        api.MapServiceOrdersEndpoints();
        api.MapRepairRequestsEndpoints();

        if (environment.IsDevelopment() && configuration.GetValue<bool>("Firestore:Enabled"))
            api.MapFirestoreDevEndpoints();

        return app;
    }
}
