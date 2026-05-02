using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace ServiceFlow.Api.IntegrationTests;

/// <summary>
/// Spins up the real API against a disposable PostgreSQL instance when Docker is available.
/// If Docker is not running, <see cref="IsEnabled"/> is false and tests should call
/// <c>Skip.IfNot(factory.IsEnabled, factory.DisabledReason)</c>.
/// </summary>
public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    public bool IsEnabled { get; private set; }

    public string? DisabledReason { get; private set; }

    public async Task InitializeAsync()
    {
        if (IsExplicitlyDisabled())
        {
            DisabledReason =
                "Integration tests skipped because RUN_INTEGRATION_TESTS is false, 0, or no. Unset it or set to true to run (Docker required).";
            IsEnabled = false;
            return;
        }

        try
        {
            _postgres = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("serviceflow_test")
                .WithUsername("test")
                .WithPassword("test")
                .Build();

            await _postgres.StartAsync();
            IsEnabled = true;
        }
        catch (DockerUnavailableException ex)
        {
            IsEnabled = false;
            DisabledReason =
                "Docker is not available. Start Docker Desktop (or set DOCKER_HOST) to run integration tests. " + ex.Message;
        }
    }

    private static bool IsExplicitlyDisabled()
    {
        var raw = Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return raw.Equals("0", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("false", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("no", StringComparison.OrdinalIgnoreCase);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (!IsEnabled || _postgres is null)
        {
            return;
        }

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ServiceFlow"] = _postgres.GetConnectionString(),
                ["Database:AutoMigrate"] = "true",
                ["Seed:AdminEmail"] = "",
                ["Seed:AdminPassword"] = "",
                ["Jwt:Issuer"] = "serviceflow-test",
                ["Jwt:Audience"] = "serviceflow-test-clients",
                ["Jwt:SigningKey"] = "integration-test-signing-key-min-32-chars!",
                ["Jwt:AccessTokenLifetimeMinutes"] = "60",
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Cors:AllowedOrigins:0"] = "http://localhost"
            });
        });
    }

    public new HttpClient CreateClient(WebApplicationFactoryClientOptions? options = null)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException(DisabledReason ?? "Integration tests are not enabled.");
        }

        return options is null ? base.CreateClient() : base.CreateClient(options);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }
}
