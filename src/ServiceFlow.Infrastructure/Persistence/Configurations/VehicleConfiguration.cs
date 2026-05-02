using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.Vehicles;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.LicensePlate).IsRequired().HasMaxLength(10);
        builder.HasIndex(x => x.LicensePlate).IsUnique();

        builder.Property(x => x.Make).IsRequired().HasMaxLength(60);
        builder.Property(x => x.Model).IsRequired().HasMaxLength(60);
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(40);
        builder.Property(x => x.Vin).HasMaxLength(20);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
