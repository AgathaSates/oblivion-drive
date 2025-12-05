using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.DriverModule;

namespace OblivionDrive.Infrastructure.Orm.DriverModule;
public class DriverOrmMapper : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasKey(driver => driver.Id);

        builder.Property(driver => driver.CompanyId)
            .IsRequired();

        builder.Property(driver => driver.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(driver => driver.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(driver => driver.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(driver => driver.Cpf)
            .IsRequired()
            .HasMaxLength(14);

        builder.Property(driver => driver.Cnh)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(driver => driver.CnhExpirationDate)
            .IsRequired();

        builder.Property(driver => driver.IsClientAlsoDriver)
            .IsRequired();

        builder.HasOne(driver => driver.Client)
            .WithMany(client => client.Drivers)
            .HasForeignKey(driver => driver.ClientId)
            .IsRequired();
    }
}