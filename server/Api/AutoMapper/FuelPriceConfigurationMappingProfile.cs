using AutoMapper;
using OblivionDrive.Api.Models.FuelPriceConfigurationModule;
using OblivionDrive.Application.FuelPriceConfigurationModule.Commands;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;

namespace OblivionDrive.Api.AutoMapper;

public class FuelPriceConfigurationMappingProfile : Profile
{
    public FuelPriceConfigurationMappingProfile()
    {
        CreateMap<UpdateFuelPriceConfigurationRequest, UpdateFuelPriceConfigurationCommand>();
        CreateMap<FuelPriceConfigurationDto, UpdateFuelPriceConfigurationResponse>();
        CreateMap<FuelPriceConfigurationDto, GetFuelPriceConfigurationResponse>();
    }
}