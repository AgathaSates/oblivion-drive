using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.RentalModule.Requests;
using OblivionDrive.Api.Models.RentalModule.Responses;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Querys;

namespace OblivionDrive.Api.AutoMapper;
public sealed class RentalModelsMappingProfile : Profile
{
    public RentalModelsMappingProfile()
    {
        CreateMap<RegisterRentalRequest, RegisterRentalCommand>();
        CreateMap<(Guid, UpdateRentalRequest), UpdateRentalCommand>()
            .ConvertUsing(src => new UpdateRentalCommand(
                src.Item1,
                src.Item2.ClientId,
                src.Item2.DriverId,
                src.Item2.VehicleId,
                src.Item2.PlanType,
                src.Item2.StartDate,
                src.Item2.ExpectedReturnDate,
                src.Item2.InsuranceDailyPricePerPerson,
                src.Item2.InsurancePersonsCount,
                src.Item2.EstimatedTotalKilometers,
                src.Item2.ServiceIds
            ));
        CreateMap<(Guid, CompleteRentalReturnRequest), CompleteRentalReturnCommand>()
            .ConvertUsing(src => new CompleteRentalReturnCommand(
                src.Item1,
                src.Item2.ActualReturnDate,
                src.Item2.InitialOdometerInKm,
                src.Item2.CurrentOdometerInKm,
                src.Item2.IsFuelTankFullOnReturn,
                src.Item2.HasDamage,
                src.Item2.CouponName
            ));
        CreateMap<RentalDTO, RegisterRentalResponse>();
        CreateMap<UpdatedRentalDTO, UpdateRentalResponse>();
        CreateMap<DetailRentalDTO, DetailRentalResponse>();
        CreateMap<DetailRentalDTO, GetRentalByIdResponse>()
            .ConvertUsing((src, dest, ctx) =>
                new GetRentalByIdResponse(
                    ctx.Mapper.Map<DetailRentalResponse>(src)));
        CreateMap<RentalsResult, GetAllRentalsResponse>()
            .ConvertUsing((src, dest, ctx) =>
                new GetAllRentalsResponse(
                    src?.Rentals?
                        .Select(r => ctx.Mapper.Map<DetailRentalResponse>(r))
                        .ToImmutableList() ?? ImmutableList<DetailRentalResponse>.Empty
                ));
        CreateMap<CompletedRentalDTO, CompleteRentalReturnResponse>();
    }
}