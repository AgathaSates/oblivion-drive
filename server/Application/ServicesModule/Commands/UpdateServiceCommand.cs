using FluentResults;
using MediatR;
using OblivionDrive.Application.ServicesModule.DTOs;
using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Application.ServicesModule.Commands;
public record UpdateServiceCommand(
    Guid ServiceId,
    string Name,
    decimal Price,
    ChargeType ChargeType
) : IRequest<Result<UpdatedServiceDTO>>;