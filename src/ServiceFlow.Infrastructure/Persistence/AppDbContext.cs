using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common.Abstractions;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public const string SchemaName = "serviceflow";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceStatusHistoryEntry> ServiceStatusHistory => Set<ServiceStatusHistoryEntry>();
    public DbSet<RepairRequest> RepairRequests => Set<RepairRequest>();
    public DbSet<RepairMedia> RepairMedia => Set<RepairMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
