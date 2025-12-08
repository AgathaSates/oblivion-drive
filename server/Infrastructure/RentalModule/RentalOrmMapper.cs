using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Infrastructure.Orm.RentalModule;
public class RentalOrmMapper : IEntityTypeConfiguration<Rental>
{
    public void Configure(EntityTypeBuilder<Rental> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CompanyId)
            .IsRequired();

        builder.Property(r => r.ClientId)
            .IsRequired();

        builder.Property(r => r.DriverId)
            .IsRequired();

        builder.Property(r => r.VehicleId)
            .IsRequired();

        builder.HasOne(r => r.Client)
             .WithMany()
             .HasForeignKey(r => r.ClientId)
             .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Driver)
            .WithMany()
            .HasForeignKey(r => r.DriverId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Vehicle)
            .WithMany()
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.NoAction);

        //Cupom
        builder.Property(r => r.CouponId);

        builder.HasOne(r => r.Coupon)
             .WithMany()
             .HasForeignKey(r => r.CouponId)
             .OnDelete(DeleteBehavior.NoAction);

        builder.Property(r => r.CouponDiscountAmount)
            .HasPrecision(18, 2);

        // Datas
        builder.Property(r => r.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(r => r.ExpectedReturnDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(r => r.ActualReturnDate)
            .HasColumnType("date");

        // Plano
        builder.Property(r => r.PlanType)
            .IsRequired();

        // Seguro
        builder.Property(r => r.InsuranceDailyPricePerPerson)
            .HasPrecision(18, 2);

        builder.Property(r => r.InsurancePersonsCount)
            .IsRequired();

        // Km
        builder.Property(r => r.InitialOdometerInKm)
            .IsRequired();

        builder.Property(r => r.CurrentOdometerInKm);

        // Caução
        builder.Property(r => r.SecurityDepositAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.HasDamage)
            .IsRequired();

        builder.Property(r => r.IsFuelTankFullOnReturn)
            .IsRequired();

        builder.Property(r => r.IsCompleted)
            .IsRequired();

        // Valores
        builder.Property(r => r.EstimatedTotalKilometers);

        builder.Property(r => r.RentalBasePrice)
            .HasPrecision(18, 2);

        builder.Property(r => r.InsuranceTotalPrice)
            .HasPrecision(18, 2);

        builder.Property(r => r.ServicesTotalPrice)
            .HasPrecision(18, 2);

        builder.Property(r => r.EstimatedRentalAmount)
            .HasPrecision(18, 2);

        builder.Property(r => r.PenaltyPrice)
            .HasPrecision(18, 2);

        builder.Property(r => r.FuelChargePrice)
            .HasPrecision(18, 2);

        builder.Property(r => r.GrossRentalAmount)
            .HasPrecision(18, 2);

        builder.Property(r => r.FinalAmountToPay)
            .HasPrecision(18, 2);

        builder.Property<HashSet<Guid>>("_serviceIds")
            .HasColumnName("ServiceIds")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                ids => SerializeServiceIds(ids),
                json => DeserializeServiceIds(json));
    }

    private static string SerializeServiceIds(HashSet<Guid> serviceIds)
    {
        if (serviceIds is null || serviceIds.Count == 0)
            return string.Empty;

        return JsonSerializer.Serialize(serviceIds);
    }

    private static HashSet<Guid> DeserializeServiceIds(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new HashSet<Guid>();

        var result = JsonSerializer.Deserialize<HashSet<Guid>>(json);
        return result ?? new HashSet<Guid>();
    }
}