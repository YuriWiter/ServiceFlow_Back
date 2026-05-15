using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Domain.Users;

namespace ServiceFlow.Infrastructure.Persistence;

/// <summary>
/// Applies pending EF migrations (relational backend only) and seeds the admin user via <see cref="IServiceFlowPersistence"/>.
/// </summary>
public sealed class AppDbContextInitializer
{
    private readonly IServiceFlowPersistence _persistence;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppDbContextInitializer> _logger;

    public AppDbContextInitializer(
        IServiceFlowPersistence persistence,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AppDbContextInitializer> logger)
    {
        _persistence = persistence;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (services.GetService<AppDbContext>() is not { } db)
        {
            return;
        }

        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingList = pending.ToList();
        if (pendingList.Count == 0)
        {
            _logger.LogInformation("Database schema is up to date.");
            return;
        }

        _logger.LogInformation("Applying {Count} pending migrations: {Migrations}", pendingList.Count, string.Join(", ", pendingList));
        await db.Database.MigrateAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = _configuration["Seed:AdminEmail"];
        var password = _configuration["Seed:AdminPassword"];
        var fullName = _configuration["Seed:AdminFullName"] ?? "System Administrator";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("No admin seed configuration present (Seed:AdminEmail / Seed:AdminPassword); skipping seed.");
            return;
        }

        var emailLower = email.Trim().ToLowerInvariant();
        if (await _persistence.UserExistsWithEmailAsync(emailLower, cancellationToken))
        {
            return;
        }

        var admin = User.Create(emailLower, fullName, _passwordHasher.Hash(password), UserRole.Admin);
        await _persistence.AddUserAsync(admin, cancellationToken);
        await _persistence.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded admin user {Email}.", emailLower);
    }
}

public static class AppDbContextInitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<AppDbContextInitializer>();
        await initializer.MigrateAsync(scope.ServiceProvider, cancellationToken);
        await initializer.SeedAsync(cancellationToken);
    }
}
