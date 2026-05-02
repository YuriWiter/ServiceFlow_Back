using ServiceFlow.Application.Auth;

namespace ServiceFlow.Api.Endpoints;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/register/customer", async (RegisterCustomerCommand command, IAuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.RegisterCustomerAsync(command, ct)))
            .WithSummary("Register a new customer account");

        group.MapPost("/register/staff", async (RegisterStaffCommand command, IAuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.RegisterStaffAsync(command, ct)))
            .RequireAuthorization(Authorization.AuthorizationPolicies.AdminOnly)
            .WithSummary("Register a new staff account (admin only)");

        group.MapPost("/login", async (LoginCommand command, IAuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.LoginAsync(command, ct)))
            .WithSummary("Exchange email + password for a JWT access token");

        return app;
    }
}
