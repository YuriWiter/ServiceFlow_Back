using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace ServiceFlow.Api.IntegrationTests;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthEndpointsTests
{
    private readonly IntegrationTestWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthEndpointsTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Register_customer_then_login_returns_tokens()
    {
        Skip.IfNot(_factory.IsEnabled, _factory.DisabledReason);

        var client = _factory.CreateClient();
        var email = $"it-{Guid.NewGuid():N}@example.com";

        var registerPayload = new
        {
            email,
            fullName = "Integration Test User",
            password = "TestPassw0rd!",
            phoneNumber = "14155550199"
        };

        var register = await client.PostAsJsonAsync("/api/v1/auth/register/customer", registerPayload);
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await register.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        registerBody.Should().NotBeNull();
        registerBody!.AccessToken.Should().NotBeNullOrWhiteSpace();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "TestPassw0rd!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await login.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        loginBody.Should().NotBeNull();
        loginBody!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    private sealed record AuthResponseDto(
        Guid UserId,
        string Email,
        string FullName,
        string Role,
        string AccessToken,
        DateTimeOffset ExpiresAt);
}
