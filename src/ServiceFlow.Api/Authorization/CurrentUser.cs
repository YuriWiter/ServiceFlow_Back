using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Api.Authorization;

internal sealed class CurrentUser : ICurrentUser
{
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(sub, out var userId))
        {
            UserId = userId;
        }

        Email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? principal.FindFirstValue(ClaimTypes.Email);

        var roleValue = principal.FindFirstValue(ClaimTypes.Role);
        if (Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var role))
        {
            Role = role;
        }
    }

    public Guid? UserId { get; }
    public UserRole? Role { get; }
    public string? Email { get; }

    public Guid RequireUserId()
    {
        return UserId ?? throw new ForbiddenException("Authentication required.", "auth.required");
    }
}
