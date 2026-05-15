using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Infrastructure.Persistence;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Application.Tests.Fakes;

/// <summary>
/// Spins up an in-memory DbContext, test-friendly fake clock, JWT signer and a real
/// validator registry sourced from the Application assembly. The fixture is disposable
/// so each test gets an isolated database.
/// </summary>
internal sealed class InMemoryAppFixture : IDisposable
{
    private readonly ServiceProvider _provider;

    public InMemoryAppFixture()
    {
        var services = new ServiceCollection();
        services.AddDbContext<InMemoryDbContext>(options => options
            .UseInMemoryDatabase($"sf-tests-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<InMemoryDbContext>());
        services.AddScoped<EfServiceFlowPersistence>();
        services.AddScoped<IServiceFlowPersistence>(sp => sp.GetRequiredService<EfServiceFlowPersistence>());
        services.AddSingleton<IDateTimeProvider, FakeDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, FakePasswordHasher>();
        services.AddSingleton<IJwtTokenService, FakeJwtTokenService>();
        services.AddSingleton<ICurrentUser, FakeCurrentUser>();
        services.AddSingleton<IFirestoreSyncService, NoOpFirestoreSyncServiceForTests>();
        services.AddSingleton<IFirestoreUserReader, NoOpFirestoreUserReaderForTests>();
        services.AddSingleton<IFirestoreMigrationSettings, NoFirestoreMigrationSettingsForTests>();
        services.AddValidatorsFromAssembly(typeof(ServiceFlow.Application.DependencyInjection).Assembly, includeInternalTypes: true);

        _provider = services.BuildServiceProvider();
        Scope = _provider.CreateScope();
    }

    public IServiceScope Scope { get; }

    public IAppDbContext Db => Scope.ServiceProvider.GetRequiredService<IAppDbContext>();
    public IServiceFlowPersistence Persistence => Scope.ServiceProvider.GetRequiredService<IServiceFlowPersistence>();
    public IPasswordHasher PasswordHasher => Scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    public IJwtTokenService JwtService => Scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
    public FakeCurrentUser CurrentUser => (FakeCurrentUser)Scope.ServiceProvider.GetRequiredService<ICurrentUser>();
    public FakeDateTimeProvider Clock => (FakeDateTimeProvider)Scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

    public IValidator<T> Validator<T>() => Scope.ServiceProvider.GetRequiredService<IValidator<T>>();
    public IFirestoreSyncService FirestoreSync => Scope.ServiceProvider.GetRequiredService<IFirestoreSyncService>();
    public IFirestoreUserReader FirestoreUserReader => Scope.ServiceProvider.GetRequiredService<IFirestoreUserReader>();
    public IFirestoreMigrationSettings FirestoreMigration => Scope.ServiceProvider.GetRequiredService<IFirestoreMigrationSettings>();

    public void Dispose()
    {
        Scope.Dispose();
        _provider.Dispose();
    }

    private sealed class InMemoryDbContext(DbContextOptions<InMemoryDbContext> options) : DbContext(options), IAppDbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
        public DbSet<ServiceStatusHistoryEntry> ServiceStatusHistory => Set<ServiceStatusHistoryEntry>();
        public DbSet<RepairRequest> RepairRequests => Set<RepairRequest>();
        public DbSet<RepairMedia> RepairMedia => Set<RepairMedia>();
    }

    private sealed class NoOpFirestoreSyncServiceForTests : IFirestoreSyncService
    {
        public Task UpsertUserAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpsertCustomerAsync(Customer customer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpsertVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpsertServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpsertRepairRequestAsync(RepairRequest repair, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpFirestoreUserReaderForTests : IFirestoreUserReader
    {
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);
    }

    private sealed class NoFirestoreMigrationSettingsForTests : IFirestoreMigrationSettings
    {
        public bool AuthReadFromFirestore => false;
    }
}
