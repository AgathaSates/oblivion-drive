using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.VehicleModule.Requests;
using OblivionDrive.Api.Models.VehicleModule.Responses;
using OblivionDrive.Application.VehicleModule.Commands;
using OblivionDrive.Application.VehicleModule.DTOs;
using OblivionDrive.Application.VehicleModule.Querys;

namespace OblivionDrive.Api.AutoMapper;

public sealed class VehiclesModelsMappingProfile : Profile
{
    public VehiclesModelsMappingProfile()
    {
        CreateMap<RegisterVehicleRequest, RegisterVehicleCommand>();
        CreateMap<VehicleDTO, RegisterVehicleResponse>();
        CreateMap<(Guid, UpdateVehicleRequest), UpdateVehicleCommand>()
            .ConvertUsing(src => new UpdateVehicleCommand(
                src.Item1,
                src.Item2.Brand,
                src.Item2.Model,
                src.Item2.Color,
                src.Item2.FuelType,
                src.Item2.FuelTankCapacityInLiters,
                src.Item2.Year,
                src.Item2.VehicleGroupId,
                src.Item2.PhotoBytes
            ));

        CreateMap<UpdatedVehicleDTO, UpdateVehicleResponse>();
        CreateMap<DetailVehicleDTO, GetVehicleByIdResponse>();
        CreateMap<VehiclesResult, GetAllVehiclesResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllVehiclesResponse(
                src.Vehicles.Count,
                src?.Vehicles?
                    .Select(v => ctx.Mapper.Map<DetailVehicleDTO>(v))
                    .ToImmutableList() ?? ImmutableList<DetailVehicleDTO>.Empty
            ));
    }
}