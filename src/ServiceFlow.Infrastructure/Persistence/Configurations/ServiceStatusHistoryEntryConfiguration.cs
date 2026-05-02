using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.ServiceOrders;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class ServiceStatusHistoryEntryConfiguration : IEntityTypeConfiguration<ServiceStatusHistoryEntry>
{
    public void Configure(EntityTypeBuilder<ServiceStatusHistoryEntry> builder)
    {
        builder.ToTable("service_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ServiceOrderId).IsRequired();
        builder.Property(x => x.FromStage).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.ToStage).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.ChangedByUserId).IsRequired();
        builder.Property(x => x.ChangedAt).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => new { x.ServiceOrderId, x.ChangedAt });
    }
}
