namespace OblivionDrive.Api.Models.BillingPlanModule;

public record UpdateBillingPlanRequest(
    string Name,
    Guid VehicleGroupId,
    decimal DailyPlanDailyRate,
    decimal DailyPlanPricePerKilometer,
    decimal ControlledPlanDailyRate,
    decimal ControlledPlanExtraPricePerKilometer,
    decimal FreePlanDailyRate);
