using AutoMapper;
using OblivionDrive.Application.ServicesModule.DTOs;
using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Application.AutoMapper;
public class ServiceMappingProfile : Profile
{
    public ServiceMappingProfile()
    {
        CreateMap<Service, ServiceDTO>()
            .ConstructUsing(service => new ServiceDTO(
                true,
                service.Name,
                service.Price,
                service.ChargeType));

        CreateMap<Service, UpdatedServiceDTO>()
            .ConstructUsing(service => new UpdatedServiceDTO(
                true,
                service.Name,
                service.Price,
                service.ChargeType));

        CreateMap<Service, DetailServiceDTO>();
    }
}