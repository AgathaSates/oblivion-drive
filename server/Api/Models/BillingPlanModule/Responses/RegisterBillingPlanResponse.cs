namespace OblivionDrive.Api.Models.BillingPlanModule;

public record RegisterBillingPlanResponse(
    bool CreatedSuccessfully,
    string Name,
    Guid VehicleGroupId,
    decimal DailyPlanDailyRate,
    decimal DailyPlanPricePerKilometer,
    decimal ControlledPlanDailyRate,
    decimal ControlledPlanExtraPricePerKilometer,
    decimal FreePlanDailyRate);
