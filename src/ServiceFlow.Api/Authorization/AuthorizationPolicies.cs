namespace ServiceFlow.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string StaffOnly = "staff-only";
    public const string AdminOnly = "admin-only";
    public const string CustomerOnly = "customer-only";
    public const string AuthenticatedUser = "authenticated";
}
