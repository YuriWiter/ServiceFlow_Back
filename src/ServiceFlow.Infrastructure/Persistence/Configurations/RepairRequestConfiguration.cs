using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.RepairRequests;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class RepairRequestConfiguration : IEntityTypeConfiguration<RepairRequest>
{
    public void Configure(EntityTypeBuilder<RepairRequest> builder)
    {
        builder.ToTable("repair_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ServiceOrderId).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.IssueDescription).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.PriceEstimateCents).IsRequired();
        builder.Property(x => x.Urgency).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CustomerDecision).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DecidedAt);
        builder.Property(x => x.DecisionNote).HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasIndex(x => x.ServiceOrderId);
        builder.HasIndex(x => x.CustomerDecision);

        builder.HasMany(x => x.Media)
            .WithOne()
            .HasForeignKey(m => m.RepairRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(RepairRequest.Media))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
