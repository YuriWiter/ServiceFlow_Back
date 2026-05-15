using Microsoft.AspNetCore.Http;
using ServiceFlow.Application.Common.Abstractions;

namespace ServiceFlow.Api.Endpoints;

internal static class FirestoreDevEndpoints
{
    internal static void MapFirestoreDevEndpoints(this RouteGroupBuilder api)
    {
        var dev = api.MapGroup("/dev").WithTags("Firestore (migration)").AllowAnonymous();
        dev.MapGet(
                "/firestore-ping",
                async Task<IResult> (IFirestoreConnectivityProbe probe, CancellationToken ct) =>
                {
                    var r = await probe.PingAsync(ct);
                    return r.Ok
                        ? Results.Ok(new { firestore = "ok" })
                        : Results.Problem(detail: r.Error, statusCode: StatusCodes.Status503ServiceUnavailable);
                })
            .WithName("DevFirestorePing")
            .WithSummary("Firestore connectivity check (Development only when Firestore is enabled)");

        dev.MapGet(
                "/firestore-users/{userId:guid}",
                async Task<IResult> (Guid userId, IFirestoreUserReader reader, CancellationToken ct) =>
                {
                    var user = await reader.GetByIdAsync(userId, ct);
                    if (user is null)
                        return Results.NotFound();
                    return Results.Ok(new
                    {
                        user.Id,
                        user.Email,
                        user.FullName,
                        user.Role,
                        user.IsActive,
                        user.CreatedAt,
                        user.UpdatedAt
                    });
                })
            .WithName("DevFirestoreGetUser")
            .WithSummary("Fetch user document from Firestore by id (no password hash)");
    }
}
