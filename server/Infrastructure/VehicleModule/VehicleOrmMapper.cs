using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Infrastructure.Orm.VehicleModule;
public class VehicleOrmMapper : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.CompanyId)
            .IsRequired();

        builder.Property(v => v.LicensePlate)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(v => v.Brand)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Model)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Color)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.FuelTankCapacityInLiters)
            .HasColumnType("decimal(10,2)");

        builder.Property(v => v.Year)
            .IsRequired();

        builder.Property(v => v.PhotoBytes)
            .HasColumnType("varbinary(max)");

        builder.HasOne(v => v.VehicleGroup)
            .WithMany()
            .HasForeignKey(v => v.VehicleGroupId)
            .IsRequired();
    }
}