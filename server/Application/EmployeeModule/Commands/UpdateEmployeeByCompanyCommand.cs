using FluentResults;
using MediatR;
using OblivionDrive.Application.EmployeeModule.DTOs;

namespace OblivionDrive.Application.EmployeeModule.Commands;
public record UpdateEmployeeByCompanyCommand(
    Guid EmployeeId,
    string Name,
    DateOnly HireDate,
    decimal Salary
) : IRequest<Result<UpdatedEmployeeDTO>>;
