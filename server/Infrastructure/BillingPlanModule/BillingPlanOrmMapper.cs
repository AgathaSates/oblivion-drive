using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.BillingPlanModule;

namespace OblivionDrive.Infrastructure.Orm.BillingPlanModule;
public class BillingPlanOrmMapper : IEntityTypeConfiguration<BillingPlan>
{
    public void Configure(EntityTypeBuilder<BillingPlan> builder)
    {
        builder.HasKey(bp => bp.Id);

        builder.Property(bp => bp.CompanyId)
                .IsRequired();

        builder.Property(bp => bp.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.HasOne(bp => bp.VehicleGroup)
            .WithMany(vg => vg.BillingPlans)
            .HasForeignKey(bp => bp.VehicleGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(bp => bp.DailyPlan, daily =>
        {
            daily.Property(p => p.DailyRate)
                .HasColumnName("DailyPlan_DailyRate")
                .HasColumnType("decimal(18,2)");

            daily.Property(p => p.PricePerKilometer)
                .HasColumnName("DailyPlan_PricePerKilometer")
                .HasColumnType("decimal(18,2)");

            daily.WithOwner();
        });

        builder.OwnsOne(bp => bp.ControlledPlan, controlled =>
        {
            controlled.Property(p => p.DailyRate)
                .HasColumnName("ControlledPlan_DailyRate")
                .HasColumnType("decimal(18,2)");

            controlled.Property(p => p.ExtraPricePerKilometer)
                .HasColumnName("ControlledPlan_ExtraPricePerKilometer")
                .HasColumnType("decimal(18,2)");

            controlled.WithOwner();
        });

        builder.OwnsOne(bp => bp.FreePlan, free =>
        {
            free.Property(p => p.DailyRate)
                .HasColumnName("FreePlan_DailyRate")
                .HasColumnType("decimal(18,2)");

            free.WithOwner();
        });

        builder.Navigation(bp => bp.DailyPlan).IsRequired();
        builder.Navigation(bp => bp.ControlledPlan).IsRequired();
        builder.Navigation(bp => bp.FreePlan).IsRequired();
    }
}