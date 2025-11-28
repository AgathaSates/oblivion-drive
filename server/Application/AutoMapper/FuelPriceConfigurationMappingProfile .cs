using AutoMapper;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;
using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Application.AutoMapper;
public class FuelPriceConfigurationMappingProfile : Profile
{
    public FuelPriceConfigurationMappingProfile()
    {
        CreateMap<FuelPriceConfiguration, FuelPriceConfigurationDto>()
            .ConstructUsing(f => new FuelPriceConfigurationDto(
                f.Gasoline,
                f.Gas,
                f.Diesel,
                f.Alcohol,
                f.LastUpdate));
    }
}