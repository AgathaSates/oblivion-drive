using FluentResults;
using MediatR;
using OblivionDrive.Application.EmployeeModule.DTOs;

namespace OblivionDrive.Application.EmployeeModule.Commands;
public record UpdateOwnEmployeeProfileCommand(
    string Name
) : IRequest<Result<UpdatedEmployeeDTO>>;