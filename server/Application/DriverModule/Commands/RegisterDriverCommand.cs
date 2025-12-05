using FluentResults;
using MediatR;
using OblivionDrive.Application.DriverModule.DTOs;

namespace OblivionDrive.Application.DriverModule.Commands;
public record RegisterDriverCommand(
    string Name,
    string Email,
    string PhoneNumber,
    string Cpf,
    string Cnh,
    DateOnly CnhExpirationDate,
    Guid ClientId,
    bool IsClientAlsoDriver
) : IRequest<Result<DriverDTO>>;