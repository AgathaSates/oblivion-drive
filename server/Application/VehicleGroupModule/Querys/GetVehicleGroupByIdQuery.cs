using FluentResults;
using MediatR;
using OblivionDrive.Application.VehicleGroupModule.DTOs;

namespace OblivionDrive.Application.VehicleGroupModule.Querys;
public record GetVehicleGroupByIdQuery(Guid VehicleGroupId) : IRequest<Result<DetailVehicleGroupDTO>>;