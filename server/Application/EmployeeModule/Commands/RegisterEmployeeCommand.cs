using FluentResults;
using MediatR;
using OblivionDrive.Application.EmployeeModule.DTOs;

namespace OblivionDrive.Application.EmployeeModule.Commands;
public record RegisterEmployeeCommand(
    string UserName,
    string Email,
    string Password,
    string Name,
    DateOnly HireDate,
    Decimal Salary
    ) : IRequest<Result<EmployeeDTO>>;