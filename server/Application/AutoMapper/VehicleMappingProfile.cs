using AutoMapper;
using OblivionDrive.Application.VehicleModule.DTOs;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Application.AutoMapper;

public class VehicleMappingProfile : Profile
{
    public VehicleMappingProfile()
    {
        CreateMap<Vehicle, VehicleDTO>()
            .ConstructUsing(vehicle => new VehicleDTO(
                true,
                vehicle.LicensePlate,
                vehicle.Brand,
                vehicle.Model,
                vehicle.Color,
                vehicle.FuelType,
                vehicle.FuelTankCapacityInLiters,
                vehicle.Year,
                vehicle.VehicleGroupId,
                vehicle.PhotoBytes ?? Array.Empty<byte>()));

        CreateMap<Vehicle, UpdatedVehicleDTO>()
            .ConstructUsing(vehicle => new UpdatedVehicleDTO(
                true,
                vehicle.LicensePlate,
                vehicle.Brand,
                vehicle.Model,
                vehicle.Color,
                vehicle.FuelType,
                vehicle.FuelTankCapacityInLiters,
                vehicle.Year,
                vehicle.VehicleGroupId,
                vehicle.PhotoBytes ?? Array.Empty<byte>()));

        CreateMap<Vehicle, DetailVehicleDTO>()
            .ConstructUsing(vehicle => new DetailVehicleDTO(
                vehicle.Id,
                vehicle.LicensePlate,
                vehicle.Brand,
                vehicle.Model,
                vehicle.Color,
                vehicle.FuelType,
                vehicle.FuelTankCapacityInLiters,
                vehicle.Year,
                vehicle.VehicleGroupId,
                vehicle.PhotoBytes ?? Array.Empty<byte>()));
    }
}