using FluentResults;
using MediatR;
using OblivionDrive.Application.DriverModule.DTOs;

namespace OblivionDrive.Application.DriverModule.Querys;
public record GetDriverByIdQuery(Guid DriverId) : IRequest<Result<DetailDriverDTO>>;