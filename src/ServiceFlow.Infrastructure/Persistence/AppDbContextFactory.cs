using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceFlow.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` tooling at design time. Reads the connection string from the
/// SERVICEFLOW_CONNECTION_STRING environment variable (falls back to a local default).
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SERVICEFLOW_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=serviceflow;Username=serviceflow;Password=serviceflow";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", AppDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
