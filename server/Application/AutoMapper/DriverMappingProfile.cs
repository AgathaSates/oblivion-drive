using AutoMapper;
using OblivionDrive.Application.DriverModule.DTOs;
using OblivionDrive.Domain.DriverModule;

namespace OblivionDrive.Application.AutoMapper;
public class DriverMappingProfile : Profile
{
    public DriverMappingProfile()
    {
        CreateMap<Driver, DriverDTO>()
            .ConstructUsing(driver => new DriverDTO(
                true,
                driver.Name,
                driver.Email,
                driver.PhoneNumber,
                driver.Cpf,
                driver.Cnh,
                driver.CnhExpirationDate,
                driver.ClientId,
                driver.IsClientAlsoDriver));

        CreateMap<Driver, UpdatedDriverDTO>()
            .ConstructUsing(driver => new UpdatedDriverDTO(
                true,
                driver.Name,
                driver.Email,
                driver.PhoneNumber,
                driver.Cpf,
                driver.Cnh,
                driver.CnhExpirationDate,
                driver.ClientId,
                driver.IsClientAlsoDriver));

        CreateMap<Driver, DetailDriverDTO>()
            .ConstructUsing(driver => new DetailDriverDTO(
                driver.Id,
                driver.Name,
                driver.Email,
                driver.PhoneNumber,
                driver.Cpf,
                driver.Cnh,
                driver.CnhExpirationDate,
                driver.ClientId,
                driver.IsClientAlsoDriver));
    }
}
