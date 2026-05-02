using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Common.Abstractions;

public interface IJwtTokenService
{
    AccessToken IssueAccessToken(User user);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
