using FluentResults;
using MediatR;

namespace OblivionDrive.Application.EmployeeModule.Commands;
public record DeleteEmployeeByCompanyCommand(Guid EmployeeId) : IRequest<Result>;