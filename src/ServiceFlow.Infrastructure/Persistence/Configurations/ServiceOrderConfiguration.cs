using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.ServiceOrders;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("service_orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.VehicleId).IsRequired();
        builder.Property(x => x.AssignedStaffId);
        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.OpeningDescription).IsRequired().HasMaxLength(2000);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.CompletedAt);

        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.AssignedStaffId);
        builder.HasIndex(x => x.Stage);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany(x => x.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RepairRequests)
            .WithOne()
            .HasForeignKey(r => r.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(ServiceOrder.StatusHistory))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata
            .FindNavigation(nameof(ServiceOrder.RepairRequests))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
