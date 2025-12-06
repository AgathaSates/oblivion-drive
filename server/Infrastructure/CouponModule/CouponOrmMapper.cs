using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.CouponModule;

namespace OblivionDrive.Infrastructure.Orm.CouponModule;
public class CouponOrmMapper : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CompanyId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Value)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(c => c.ExpirationDate)
            .IsRequired();

        builder.Property(c => c.PartnerId)
            .IsRequired();

        builder.HasOne(c => c.Partner)
            .WithMany(p => p.Coupons)
            .HasForeignKey(c => c.PartnerId)
            .IsRequired();
    }
}