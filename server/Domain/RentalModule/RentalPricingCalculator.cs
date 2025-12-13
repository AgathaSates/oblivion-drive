using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Domain.RentalModule;
public class RentalPricingCalculator
{
    public decimal CalculateOnCreation(decimal rentalBasePrice, decimal insuranceTotalPrice, decimal servicesTotalPrice)
    {
        decimal estimatedRentalAmount = rentalBasePrice + insuranceTotalPrice + servicesTotalPrice;

        return estimatedRentalAmount;
    }

    public virtual decimal CalculateOnReturn(decimal rentalBasePrice, decimal insuranceTotalPrice,
       decimal servicesTotalPrice, decimal fuelChargePrice, decimal penaltyPrice)
    {
        decimal grossRentalAmount =
            rentalBasePrice +
            insuranceTotalPrice +
            servicesTotalPrice +
            fuelChargePrice +
            penaltyPrice;

        return grossRentalAmount;
    }

    // Serviços
    public virtual decimal CalculateServicesTotalPrice(IReadOnlyCollection<Service> selectedServices, int rentalDays)
    {
        if (selectedServices is null || selectedServices.Count == 0)
            return 0m;

        if (rentalDays <= 0)
            return 0m;

        decimal totalServicesPrice = 0m;

        foreach (Service service in selectedServices)
        {
            if (service is null)
                continue;

            decimal serviceTotal = service.ChargeType switch
            {
                ChargeType.Fixed => service.Price,
                ChargeType.perDay => service.Price * rentalDays,
                _ => 0m,
            };

            totalServicesPrice += serviceTotal;
        }

        return totalServicesPrice;
    }

    // Seguro
    public virtual decimal CalculateInsuranceTotalPrice(decimal insuranceDailyPricePerPerson, int insurancePersonsCount, int rentalDays)
    {
        if (rentalDays <= 0)
            return 0m;

        return insuranceDailyPricePerPerson * insurancePersonsCount * rentalDays;
    }

    // Plano
    public decimal CalculateEstimatedDailyRentalAmount(BillingPlan billingPlan, RentalPlanType planType,
    DateOnly startDate, DateOnly expectedReturnDate)
    {
        int rentalDays = CalculateRentalDays(startDate, expectedReturnDate);

        decimal dailyRate = planType switch
        {
            RentalPlanType.Daily => billingPlan.DailyPlan.DailyRate,
            RentalPlanType.Controlled => billingPlan.ControlledPlan.DailyRate,
            RentalPlanType.Free => billingPlan.FreePlan.DailyRate,
            _ => 0m
        };

        return rentalDays * dailyRate;
    }

    public virtual decimal CalculateFinalRentalAmountOnReturn(
    BillingPlan billingPlan, RentalPlanType planType,
    DateOnly startDate, DateOnly actualReturnDate,
    int initialOdometerInKilometers, int currentOdometerInKilometers,int? estimatedTotalKilometers)
    {
        int rentalDays = CalculateRentalDays(startDate, actualReturnDate);

        int traveledKilometers = Math.Max(0, currentOdometerInKilometers - initialOdometerInKilometers);

        int extraKilometers = 0;

        if (planType == RentalPlanType.Controlled && estimatedTotalKilometers.HasValue)
        {
            extraKilometers = Math.Max(0, traveledKilometers - estimatedTotalKilometers.Value);
        }

        return planType switch
        {
            RentalPlanType.Daily =>
                billingPlan.DailyPlan.DailyRate * rentalDays +
                billingPlan.DailyPlan.PricePerKilometer * traveledKilometers,

            RentalPlanType.Controlled =>
                billingPlan.ControlledPlan.DailyRate * rentalDays +
                billingPlan.ControlledPlan.ExtraPricePerKilometer * extraKilometers,

            RentalPlanType.Free =>
                billingPlan.FreePlan.DailyRate * rentalDays,

            _ => 0m
        };
    }

    // Combustivel
    public decimal CalculateFuelChargePrice(Vehicle vehicle, FuelPriceConfiguration fuelPriceConfiguration, bool isFuelTankFullOnReturn)
    {
        if (isFuelTankFullOnReturn)
            return 0m;

        decimal pricePerLiter = vehicle.FuelType switch
        {
            FuelType.Gasoline => fuelPriceConfiguration.Gasoline,
            FuelType.Gas => fuelPriceConfiguration.Gas,
            FuelType.Diesel => fuelPriceConfiguration.Diesel,
            FuelType.Alcohol => fuelPriceConfiguration.Alcohol,
            _ => 0m
        };

        if (pricePerLiter <= 0m || vehicle.FuelTankCapacityInLiters <= 0m)
            return 0m;

        return pricePerLiter * vehicle.FuelTankCapacityInLiters;
    }

    // Multa
    public virtual decimal CalculateLateReturnPenalty(DateOnly expectedReturnDate, DateOnly actualReturnDate, decimal rentalBasePrice)
    {
        if (actualReturnDate <= expectedReturnDate)
            return 0m;

        if (rentalBasePrice <= 0m)
            return 0m;

        const decimal LatePenaltyPercentage = 0.10m;

        return rentalBasePrice * LatePenaltyPercentage;
    }

    public virtual int CalculateRentalDays(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            return 0;

        int days = (endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).Days;

        return days <= 0 ? 1 : days;
    }

}