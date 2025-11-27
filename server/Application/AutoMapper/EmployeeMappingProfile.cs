using AutoMapper;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Application.EmployeeModule.Querys;
using OblivionDrive.Domain.EmployeeModule;

namespace OblivionDrive.Application.AutoMapper;
internal class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        CreateMap<Employee, EmployeeDTO>()
            .ForMember(dest => dest.CreatedSuccessfully,
                opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.IdentityUser.UserName ?? string.Empty));

        CreateMap<Employee, UpdatedEmployeeDTO>()
            .ForMember(dest => dest.UpdatedSuccessfully,
                opt => opt.MapFrom(_ => true));

        CreateMap<Employee, DetailEmployeeDTO>();
    }
}