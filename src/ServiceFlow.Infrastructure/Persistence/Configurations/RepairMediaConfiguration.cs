using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.RepairRequests;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class RepairMediaConfiguration : IEntityTypeConfiguration<RepairMedia>
{
    public void Configure(EntityTypeBuilder<RepairMedia> builder)
    {
        builder.ToTable("repair_media");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.RepairRequestId).IsRequired();
        builder.Property(x => x.MediaType).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(500);
        builder.Property(x => x.MimeType).HasMaxLength(100);
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();

        builder.HasIndex(x => x.RepairRequestId);
    }
}
