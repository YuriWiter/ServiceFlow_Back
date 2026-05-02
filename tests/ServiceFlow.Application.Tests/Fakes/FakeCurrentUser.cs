using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Exceptions;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Tests.Fakes;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; } = Guid.NewGuid();
    public UserRole? Role { get; set; } = UserRole.Staff;
    public string? Email { get; set; } = "test@example.com";

    public Guid RequireUserId() => UserId ?? throw new ForbiddenException("not authenticated");
}
