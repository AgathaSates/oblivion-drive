using FluentResults;
using MediatR;
using OblivionDrive.Application.VehicleModule.DTOs;

namespace OblivionDrive.Application.VehicleModule.Querys;
public record GetVehicleByIdQuery(Guid VehicleId) : IRequest<Result<DetailVehicleDTO>>;