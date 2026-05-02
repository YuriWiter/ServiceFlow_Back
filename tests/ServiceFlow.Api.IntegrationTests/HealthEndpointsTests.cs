using System.Net;
using FluentAssertions;

namespace ServiceFlow.Api.IntegrationTests;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class HealthEndpointsTests
{
    private readonly IntegrationTestWebAppFactory _factory;

    public HealthEndpointsTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Live_returns_ok()
    {
        Skip.IfNot(_factory.IsEnabled, _factory.DisabledReason);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/v1/health/live", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
