using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.VehicleGroupModule;
using OblivionDrive.Api.Models.VehicleGroupModule.Requests;
using OblivionDrive.Api.Models.VehicleGroupModule.Responses;
using OblivionDrive.Application.VehicleGroupModule.commands;
using OblivionDrive.Application.VehicleGroupModule.DTOs;
using OblivionDrive.Application.VehicleGroupModule.Querys;

namespace OblivionDrive.Api.AutoMapper;

public class VehicleGroupsModelsMappingProfile : Profile
{
    public VehicleGroupsModelsMappingProfile()
    {
        CreateMap<RegisterVehicleGroupRequest, RegisterVehicleGroupCommand>();
        CreateMap<VehicleGroupDTO, RegisterVehicleGroupResponse>();
        CreateMap<(Guid, UpdateVehicleGroupRequest), UpdateVehicleGroupCommand>()
            .ConvertUsing(src => new UpdateVehicleGroupCommand(
                src.Item1,
                src.Item2.Name
                ));
        CreateMap<UpdatedVehicleGroupDTO, UpdateVehicleGroupResponse>();
        CreateMap<DetailVehicleGroupDTO, GetVehicleGroupByIdResponse>();
        CreateMap<VehicleGroupResult, GetAllVehicleGroupResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllVehicleGroupResponse(
                src.VehicleGroups.Count,
                src?.VehicleGroups?
                    .Select(vg => ctx.Mapper.Map<DetailVehicleGroupDTO>(vg))
                    .ToImmutableList() ?? ImmutableList<DetailVehicleGroupDTO>.Empty
            ));
    }
}