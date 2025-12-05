using AutoMapper;
using OblivionDrive.Application.BillingPlanModule.DTOs;
using OblivionDrive.Domain.BillingPlanModule;

namespace OblivionDrive.Application.AutoMapper;
public class BillingPlanMappingProfile : Profile
{
    public BillingPlanMappingProfile()
    {
        CreateMap<BillingPlan, BillingPlanDTO>()
            .ConstructUsing(billingPlan => new BillingPlanDTO(
                true,
                billingPlan.Name,
                billingPlan.VehicleGroupId,
                billingPlan.DailyPlan.DailyRate,
                billingPlan.DailyPlan.PricePerKilometer,
                billingPlan.ControlledPlan.DailyRate,
                billingPlan.ControlledPlan.ExtraPricePerKilometer,
                billingPlan.FreePlan.DailyRate));

        CreateMap<BillingPlan, UpdatedBillingPlanDTO>()
            .ConstructUsing(billingPlan => new UpdatedBillingPlanDTO(
                true,
                billingPlan.Name,
                billingPlan.VehicleGroupId,
                billingPlan.DailyPlan.DailyRate,
                billingPlan.DailyPlan.PricePerKilometer,
                billingPlan.ControlledPlan.DailyRate,
                billingPlan.ControlledPlan.ExtraPricePerKilometer,
                billingPlan.FreePlan.DailyRate));

        CreateMap<BillingPlan, DetailBillingPlanDTO>()
            .ConstructUsing(billingPlan => new DetailBillingPlanDTO(
                billingPlan.Id,
                billingPlan.Name,
                billingPlan.VehicleGroupId,
                billingPlan.DailyPlan.DailyRate,
                billingPlan.DailyPlan.PricePerKilometer,
                billingPlan.ControlledPlan.DailyRate,
                billingPlan.ControlledPlan.ExtraPricePerKilometer,
                billingPlan.FreePlan.DailyRate));
    }
}