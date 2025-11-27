using FluentResults;
using MediatR;
using OblivionDrive.Application.EmployeeModule.DTOs;

namespace OblivionDrive.Application.EmployeeModule.Querys;
public record GetEmployeeByIdForCompanyQuery(Guid EmployeeId) : IRequest<Result<DetailEmployeeDTO>>;