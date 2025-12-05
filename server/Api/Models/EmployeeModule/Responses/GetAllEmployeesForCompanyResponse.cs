using System.Collections.Immutable;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Api.Models.EmployeeModule;

public record GetAllEmployeesForCompanyResponse(int Quantity, ImmutableList<DetailEmployeeDTO> employees); public record UpdateClientResponse(
    bool UpdatedSuccessfully,
    string Name,
    string Email,
    string PhoneNumber,
    ClientType ClientType,
    string? Cpf,
    string? Rg,
    string? Cnh,
    string? Cnpj,
    string State,
    string City,
    string District,
    string Street,
    string Number
);