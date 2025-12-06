using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.PartnerModule.Requests;
using OblivionDrive.Api.Models.PartnerModule.Responses;
using OblivionDrive.Application.PartnerModule.Commands;
using OblivionDrive.Application.PartnerModule.DTOs;
using OblivionDrive.Application.PartnerModule.Querys;

namespace OblivionDrive.Api.AutoMapper;

public sealed class PartnersModelsMappingProfile : Profile
{
    public PartnersModelsMappingProfile()
    {
        CreateMap<RegisterPartnerRequest, RegisterPartnerCommand>();
        CreateMap<(Guid, UpdatePartnerRequest), UpdatePartnerCommand>()
            .ConvertUsing(src => new UpdatePartnerCommand(
                src.Item1,
                src.Item2.Name
            ));

        CreateMap<PartnerDTO, RegisterPartnerResponse>();
        CreateMap<UpdatedPartnerDTO, UpdatePartnerResponse>();
        CreateMap<DetailPartnerDTO, GetPartnerByIdResponse>();
        CreateMap<PartnersResult, GetAllPartnersResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllPartnersResponse(
                Quantity: src.Partners.Count,
                Partners: src?.Partners?
                    .Select(p => ctx.Mapper.Map<DetailPartnerDTO>(p))
                    .ToImmutableList() ?? ImmutableList<DetailPartnerDTO>.Empty
            ));
    }
}