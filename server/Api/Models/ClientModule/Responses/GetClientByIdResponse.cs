using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Api.Models.ClientModule.Responses;

public record GetClientByIdResponse(
    Guid Id,
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