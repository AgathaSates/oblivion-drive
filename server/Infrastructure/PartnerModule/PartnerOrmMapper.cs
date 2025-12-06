using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.PartnerModule;

namespace OblivionDrive.Infrastructure.Orm.PartnerModule;
public class PartnerOrmMapper : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CompanyId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(p => p.Coupons)
            .WithOne(c => c.Partner)
            .HasForeignKey(c => c.PartnerId)
            .IsRequired();
    }
}