using AutoMapper;
using OblivionDrive.Application.VehicleGroupModule.DTOs;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Application.AutoMapper;
public class VehicleGroupMappingProfile : Profile
{
    public VehicleGroupMappingProfile()
    {
        CreateMap<VehicleGroup, VehicleGroupDTO>()
            .ConstructUsing(vehicleGroup => new VehicleGroupDTO(
                true,
                vehicleGroup.Name));

        CreateMap<VehicleGroup, UpdatedVehicleGroupDTO>()
            .ConstructUsing(vehicleGroup => new UpdatedVehicleGroupDTO(
                true,
                vehicleGroup.Name));

        CreateMap<VehicleGroup, DetailVehicleGroupDTO>();
    }
}
