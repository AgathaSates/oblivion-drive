namespace OblivionDrive.Application.BillingPlanModule.DTOs;
public record BillingPlanDTO(
    bool CreatedSuccessfully,
    string Name,
    Guid VehicleGroupId,
    decimal DailyPlanDailyRate,
    decimal DailyPlanPricePerKilometer,
    decimal ControlledPlanDailyRate,
    decimal ControlledPlanExtraPricePerKilometer,
    decimal FreePlanDailyRate);