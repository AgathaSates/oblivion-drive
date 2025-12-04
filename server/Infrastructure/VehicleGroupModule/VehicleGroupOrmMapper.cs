using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Infrastructure.Orm.VehicleGroupModule;
public class VehicleGroupOrmMapper : IEntityTypeConfiguration<VehicleGroup>
{
    public void Configure(EntityTypeBuilder<VehicleGroup> builder)
    {
        builder.HasKey(vg => vg.Id);

        builder.Property(vg => vg.CompanyId)
            .IsRequired();

        builder.Property(vg => vg.Name)
            .IsRequired()
            .HasMaxLength(200);
    }
}
