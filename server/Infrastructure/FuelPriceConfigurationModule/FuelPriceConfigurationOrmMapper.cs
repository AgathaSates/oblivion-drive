using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Infrastructure.Orm.FuelPriceConfigurationModule;
public class FuelPriceConfigurationOrmMapper : IEntityTypeConfiguration<FuelPriceConfiguration>
{
    public void Configure(EntityTypeBuilder<FuelPriceConfiguration> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.CompanyId)
            .IsRequired();

        builder.Property(f => f.Gasoline)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(f => f.Gas)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(f => f.Diesel)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(f => f.Alcohol)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(f => f.LastUpdate)
            .IsRequired();
    }
}