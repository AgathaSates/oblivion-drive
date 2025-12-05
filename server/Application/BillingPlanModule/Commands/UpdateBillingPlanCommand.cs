using FluentResults;
using MediatR;
using OblivionDrive.Application.BillingPlanModule.DTOs;

namespace OblivionDrive.Application.BillingPlanModule.Commands;
public record UpdateBillingPlanCommand(
    Guid BillingPlanId,
    string Name,
    Guid VehicleGroupId,
    decimal DailyPlanDailyRate,
    decimal DailyPlanPricePerKilometer,
    decimal ControlledPlanDailyRate,
    decimal ControlledPlanExtraPricePerKilometer,
    decimal FreePlanDailyRate
) : IRequest<Result<UpdatedBillingPlanDTO>>;