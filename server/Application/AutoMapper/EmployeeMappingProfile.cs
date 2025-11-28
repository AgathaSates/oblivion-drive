using AutoMapper;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Domain.EmployeeModule;

namespace OblivionDrive.Application.AutoMapper;
internal class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        CreateMap<Employee, EmployeeDTO>()
           .ConstructUsing(employee => new EmployeeDTO(
               true,
               employee.Name,
               employee.IdentityUser.UserName ?? string.Empty));

        CreateMap<Employee, UpdatedEmployeeDTO>()
            .ConstructUsing(employee => new UpdatedEmployeeDTO(
                true,
                employee.Name,
                employee.HireDate,
                employee.Salary));

        CreateMap<Employee, DetailEmployeeDTO>();
    }
}