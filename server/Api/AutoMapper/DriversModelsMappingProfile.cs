using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.DriverModule.Requests;
using OblivionDrive.Api.Models.DriverModule.Responses;
using OblivionDrive.Application.DriverModule.Commands;
using OblivionDrive.Application.DriverModule.DTOs;
using OblivionDrive.Application.DriverModule.Querys;

namespace OblivionDrive.Api.AutoMapper;

public sealed class DriversModelsMappingProfile : Profile
{
    public DriversModelsMappingProfile()
    {
        CreateMap<RegisterDriverRequest, RegisterDriverCommand>();
        CreateMap<DriverDTO, RegisterDriverResponse>();
        CreateMap<(Guid, UpdateDriverRequest), UpdateDriverCommand>()
            .ConvertUsing(src => new UpdateDriverCommand(
                src.Item1,
                src.Item2.Name,
                src.Item2.Email,
                src.Item2.PhoneNumber,
                src.Item2.Cpf,
                src.Item2.Cnh,
                src.Item2.CnhExpirationDate,
                src.Item2.ClientId,
                src.Item2.IsClientAlsoDriver
            ));

        CreateMap<UpdatedDriverDTO, UpdateDriverResponse>();
        CreateMap<DetailDriverDTO, GetDriverByIdResponse>();
        CreateMap<DriversResult, GetAllDriversResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllDriversResponse(
                src.Drivers.Count,
                src?.Drivers?
                    .Select(driver => ctx.Mapper.Map<DetailDriverDTO>(driver))
                    .ToImmutableList() ?? ImmutableList<DetailDriverDTO>.Empty
            ));
    }
}