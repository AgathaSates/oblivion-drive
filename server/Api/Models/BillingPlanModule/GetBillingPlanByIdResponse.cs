namespace OblivionDrive.Api.Models.BillingPlanModule;

public record GetBillingPlanByIdResponse(
    Guid Id,
    string Name,
    Guid VehicleGroupId,
    decimal DailyPlanDailyRate,
    decimal DailyPlanPricePerKilometer,
    decimal ControlledPlanDailyRate,
    decimal ControlledPlanExtraPricePerKilometer,
    decimal FreePlanDailyRate);
