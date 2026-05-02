using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Application.Tests.Fakes;

internal sealed class FakeJwtTokenService : IJwtTokenService
{
    public AccessToken IssueAccessToken(User user)
        => new($"fake-token::{user.Id}", DateTimeOffset.UtcNow.AddHours(1));
}
