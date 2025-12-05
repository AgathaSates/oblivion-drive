namespace OblivionDrive.Application.BillingPlanModule.DTOs;
public record UpdatedBillingPlanDTO(
    bool UpdatedSuccessfully,
    string Name,
    Guid VehicleGroupId,
    decimal DailyPlanDailyRate,
    decimal DailyPlanPricePerKilometer,
    decimal ControlledPlanDailyRate,
    decimal ControlledPlanExtraPricePerKilometer,
    decimal FreePlanDailyRate);