using Microsoft.EntityFrameworkCore;
using ServiceFlow.Domain.Customers;
using ServiceFlow.Domain.RepairRequests;
using ServiceFlow.Domain.ServiceOrders;
using ServiceFlow.Domain.Users;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Application.Common.Abstractions;

/// <summary>
/// Abstraction over the EF Core DbContext so the Application layer can stay free of
/// Infrastructure details. Only the DbSets and SaveChanges are exposed.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<ServiceOrder> ServiceOrders { get; }
    DbSet<ServiceStatusHistoryEntry> ServiceStatusHistory { get; }
    DbSet<RepairRequest> RepairRequests { get; }
    DbSet<RepairMedia> RepairMedia { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
