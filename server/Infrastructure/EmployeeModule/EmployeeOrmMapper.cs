using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.EmployeeModule;

namespace OblivionDrive.Infrastructure.Orm.EmployeeModule;
public class EmployeeOrmMApper : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompanyId)
            .IsRequired();

        builder.Property(e => e.IdentityUserId)
            .IsRequired();

        builder.HasOne(e => e.IdentityUser)
            .WithMany()
            .HasForeignKey(e => e.IdentityUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.HireDate)
            .IsRequired();

        builder.Property(e => e.Salary)
            .IsRequired()
            .HasPrecision(18, 2);
    }
}