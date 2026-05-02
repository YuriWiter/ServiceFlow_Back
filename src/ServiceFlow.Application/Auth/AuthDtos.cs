using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Auth;

public sealed record RegisterStaffCommand(string Email, string FullName, string Password, UserRole Role);

public sealed record RegisterCustomerCommand(
    string Email,
    string FullName,
    string Password,
    string PhoneNumber);

public sealed record LoginCommand(string Email, string Password);

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    UserRole Role,
    string AccessToken,
    DateTimeOffset ExpiresAt);
