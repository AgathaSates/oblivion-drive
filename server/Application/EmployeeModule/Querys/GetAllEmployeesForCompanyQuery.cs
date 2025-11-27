using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.EmployeeModule.DTOs;

namespace OblivionDrive.Application.EmployeeModule.Querys;
public record GetAllEmployeesForCompanyQuery(int? Quantity)
    : IRequest<Result<EmployeesResult>>;

public record EmployeesResult(ImmutableList<DetailEmployeeDTO> Employees);