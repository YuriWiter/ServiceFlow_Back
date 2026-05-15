using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Google.Cloud.Firestore;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Application.Common.Configuration;
using ServiceFlow.Infrastructure.Auth;
using ServiceFlow.Infrastructure.Persistence;
using ServiceFlow.Infrastructure.Persistence.Firestore;
using ServiceFlow.Infrastructure.Time;

namespace ServiceFlow.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ServiceFlow";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateOnStart();

        var persistenceBackend = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()?.Backend
            ?? PersistenceBackend.Relational;

        if (persistenceBackend == PersistenceBackend.Firestore)
        {
            if (!configuration.GetValue<bool>($"{FirestoreOptions.SectionName}:Enabled"))
            {
                throw new InvalidOperationException(
                    "Persistence:Backend is Firestore but Firestore:Enabled is false. Enable Firestore or set Persistence:Backend to Relational.");
            }

            if (string.IsNullOrWhiteSpace(configuration[$"{FirestoreOptions.SectionName}:ProjectId"]))
            {
                throw new InvalidOperationException(
                    "Firestore:ProjectId is required when Persistence:Backend is Firestore.");
            }
        }

        if (persistenceBackend == PersistenceBackend.Relational)
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Missing connection string '{ConnectionStringName}' in configuration.");

            services.AddDbContext<AppDbContext>(options =>
            {
                options
                    .UseNpgsql(connectionString, npgsql =>
                    {
                        npgsql.MigrationsHistoryTable("__ef_migrations_history", AppDbContext.SchemaName);
                        npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                    })
                    .UseSnakeCaseNamingConvention();
            });

            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
            services.AddScoped<EfServiceFlowPersistence>();
            services.AddScoped<IServiceFlowPersistence>(sp => sp.GetRequiredService<EfServiceFlowPersistence>());
        }
        else
        {
            services.AddScoped<FirestoreServiceFlowPersistence>();
            services.AddScoped<IServiceFlowPersistence>(sp => sp.GetRequiredService<FirestoreServiceFlowPersistence>());
        }

        services.AddScoped<AppDbContextInitializer>();

        ConfigureFirestore(services, configuration, persistenceBackend);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }

    private static void ConfigureFirestore(
        IServiceCollection services,
        IConfiguration configuration,
        PersistenceBackend persistenceBackend)
    {
        services.AddOptions<FirestoreOptions>()
            .Bind(configuration.GetSection(FirestoreOptions.SectionName))
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.ProjectId),
                "Firestore:ProjectId is required when Firestore:Enabled is true.")
            .Validate(
                o => !o.SyncWrites || o.Enabled,
                "Firestore:SyncWrites requires Firestore:Enabled to be true.")
            .ValidateOnStart();

        services.AddSingleton<IFirestoreMigrationSettings, FirestoreMigrationSettings>();

        var firestoreConfigured = configuration.GetValue<bool>($"{FirestoreOptions.SectionName}:Enabled");

        if (firestoreConfigured || persistenceBackend == PersistenceBackend.Firestore)
        {
            services.AddSingleton(provider =>
            {
                var opts = provider.GetRequiredService<IOptions<FirestoreOptions>>().Value;
                return FirestoreDb.Create(opts.ProjectId.Trim());
            });

            services.AddSingleton<IFirestoreConnectivityProbe, FirestoreConnectivityProbe>();
            services.AddSingleton<IFirestoreUserReader, FirestoreUserReader>();

            var syncWrites = configuration.GetValue<bool>($"{FirestoreOptions.SectionName}:SyncWrites");
            if (syncWrites && persistenceBackend == PersistenceBackend.Relational)
            {
                services.AddSingleton<IFirestoreSyncService, FirestoreSyncService>();
            }
            else
            {
                services.AddSingleton<IFirestoreSyncService, NoOpFirestoreSyncService>();
            }
        }
        else
        {
            services.AddSingleton<IFirestoreUserReader, NoOpFirestoreUserReader>();
            services.AddSingleton<IFirestoreSyncService, NoOpFirestoreSyncService>();
        }
    }
}
