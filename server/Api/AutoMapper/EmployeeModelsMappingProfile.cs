using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.EmployeeModule;
using OblivionDrive.Api.Models.EmployeeModule.Requests;
using OblivionDrive.Api.Models.EmployeeModule.Responses;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Application.EmployeeModule.Querys;

namespace OblivionDrive.Api.AutoMapper;

public class EmployeeModelsMappingProfile : Profile
{
    public EmployeeModelsMappingProfile() 
    {
        CreateMap<RegisterEmployeeRequest, RegisterEmployeeCommand>();
        CreateMap<UpdateOwnEmployeeRequest, UpdateOwnEmployeeProfileCommand>();
        CreateMap<(Guid, UpdateEmployeeByCompanyRequest), UpdateEmployeeByCompanyCommand>()
           .ConvertUsing(src => new UpdateEmployeeByCompanyCommand(
                src.Item1,
                src.Item2.Name,
                src.Item2.HireDate,
                src.Item2.Salary
               ));

        CreateMap<EmployeeDTO, RegisterEmployeeResponse>();
        CreateMap<UpdatedEmployeeDTO, UpdateEmployeeByCompanyResponse>();
        CreateMap<UpdatedEmployeeDTO, UpdateOwnEmployeeResponse>();
        CreateMap<DetailEmployeeDTO, GetEmployeeByCompanyResponse>();
        CreateMap<EmployeesResult, GetAllEmployeesForCompanyResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllEmployeesForCompanyResponse(
                src.Employees.Count,
                src?.Employees?
                    .Select(e => ctx.Mapper.Map<DetailEmployeeDTO>(e))
                    .ToImmutableList() ?? ImmutableList<DetailEmployeeDTO>.Empty
            ));
    }
}