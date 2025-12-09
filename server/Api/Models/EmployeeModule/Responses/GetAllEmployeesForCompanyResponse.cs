using System.Collections.Immutable;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Api.Models.EmployeeModule;

public record GetAllEmployeesForCompanyResponse(int Quantity, ImmutableList<DetailEmployeeDTO> employees);