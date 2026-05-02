using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Common.Abstractions;

/// <summary>
/// Represents the authenticated principal of the current request. Implementations are
/// provided by the Api layer and scoped per-request.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    UserRole? Role { get; }
    string? Email { get; }

    bool IsAuthenticated => UserId is not null;
    bool IsStaff => Role is UserRole.Staff or UserRole.Admin;
    bool IsAdmin => Role is UserRole.Admin;
    bool IsCustomer => Role is UserRole.Customer;

    /// <summary>Throws <see cref="Exceptions.ForbiddenException"/> when the current user is not authenticated.</summary>
    Guid RequireUserId();
}
