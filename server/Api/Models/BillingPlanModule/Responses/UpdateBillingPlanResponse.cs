namespace OblivionDrive.Api.Models.BillingPlanModule;

public record UpdateBillingPlanResponse(
    bool UpdatedSuccessfully,
    string Name,
    Guid VehicleGroupId,
    decimal DailyPlanDailyRate,
    decimal DailyPlanPricePerKilometer,
    decimal ControlledPlanDailyRate,
    decimal ControlledPlanExtraPricePerKilometer,
    decimal FreePlanDailyRate);