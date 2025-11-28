using AutoMapper;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;
using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Application.AutoMapper;
public class FuelPriceConfigurationMappingProfile : Profile
{
    public FuelPriceConfigurationMappingProfile()
    {
        CreateMap<FuelPriceConfiguration, FuelPriceConfigurationDto>();
    }
}