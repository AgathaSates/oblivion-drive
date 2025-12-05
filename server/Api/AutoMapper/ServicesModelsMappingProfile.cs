using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.ServicesModule;
using OblivionDrive.Api.Models.ServicesModule.Requests;
using OblivionDrive.Api.Models.ServicesModule.Responses;
using OblivionDrive.Application.ServicesModule.Commands;
using OblivionDrive.Application.ServicesModule.DTOs;
using OblivionDrive.Application.ServicesModule.Querys;

namespace OblivionDrive.Api.AutoMapper;

public class ServicesModelsMappingProfile : Profile
{
    public ServicesModelsMappingProfile()
    {
        CreateMap<RegisterServiceRequest, RegisterServiceCommand>();
        CreateMap<ServiceDTO, RegisterServiceResponse>();
        CreateMap<(Guid, UpdateServiceRequest), UpdateServiceCommand>()
            .ConvertUsing(src => new UpdateServiceCommand(
                src.Item1,
                src.Item2.Name,
                src.Item2.Price,
                src.Item2.ChargeType
                ));

        CreateMap<UpdatedServiceDTO, UpdateServiceResponse>();
        CreateMap<DetailServiceDTO, GetServiceByIdResponse>();
        CreateMap<ServicesResult, GetAllServicesResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllServicesResponse(
                src.Services.Count,
                src?.Services?
                    .Select(s => ctx.Mapper.Map<DetailServiceDTO>(s))
                    .ToImmutableList() ?? ImmutableList<DetailServiceDTO>.Empty
            ));
    }
}