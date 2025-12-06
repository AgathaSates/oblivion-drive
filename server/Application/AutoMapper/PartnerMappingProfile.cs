using AutoMapper;
using OblivionDrive.Application.PartnerModule.DTOs;
using OblivionDrive.Domain.PartnerModule;

namespace OblivionDrive.Application.AutoMapper;
public class PartnerMappingProfile : Profile
{
    public PartnerMappingProfile()
    {
        CreateMap<Partner, PartnerDTO>()
            .ConstructUsing(partner => new PartnerDTO(
                true,
                partner.Name
            ));

        CreateMap<Partner, UpdatedPartnerDTO>()
            .ConstructUsing(partner => new UpdatedPartnerDTO(
                true,
                partner.Name
            ));

        CreateMap<Partner, DetailPartnerDTO>()
            .ConstructUsing(partner => new DetailPartnerDTO(
                partner.Id,
                partner.Name
            ));
    }
}