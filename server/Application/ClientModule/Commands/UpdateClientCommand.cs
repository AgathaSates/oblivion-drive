using FluentResults;
using MediatR;
using OblivionDrive.Application.ClientModule.DTOs;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Application.ClientModule.Commands;
public record UpdateClientCommand(
    Guid ClientId,
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
) : IRequest<Result<UpdatedClientDTO>>;