using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.BillingPlanModule;
using OblivionDrive.Application.BillingPlanModule.Commands;
using OblivionDrive.Application.BillingPlanModule.DTOs;
using OblivionDrive.Application.BillingPlanModule.Querys;

namespace OblivionDrive.Api.AutoMapper;


public sealed class BillingPlansModelsMappingProfile : Profile
{
    public BillingPlansModelsMappingProfile()
    {
        CreateMap<RegisterBillingPlanRequest, RegisterBillingPlanCommand>();
        CreateMap<BillingPlanDTO, RegisterBillingPlanResponse>();
        CreateMap<(Guid, UpdateBillingPlanRequest), UpdateBillingPlanCommand>()
            .ConvertUsing(src => new UpdateBillingPlanCommand(
                src.Item1,
                src.Item2.Name,
                src.Item2.VehicleGroupId,
                src.Item2.DailyPlanDailyRate,
                src.Item2.DailyPlanPricePerKilometer,
                src.Item2.ControlledPlanDailyRate,
                src.Item2.ControlledPlanExtraPricePerKilometer,
                src.Item2.FreePlanDailyRate
            ));

        CreateMap<UpdatedBillingPlanDTO, UpdateBillingPlanResponse>();
        CreateMap<DetailBillingPlanDTO, GetBillingPlanByIdResponse>();
        CreateMap<BillingPlanResult, GetAllBillingPlansResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllBillingPlansResponse(
                    src.BillingPlans.Count,
                    src?.BillingPlans?
                        .Select(bp => ctx.Mapper.Map<DetailBillingPlanDTO>(bp))
                        .ToImmutableList() ?? ImmutableList<DetailBillingPlanDTO>.Empty
                ));
    }
}