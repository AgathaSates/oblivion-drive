using FluentResults;
using MediatR;
using OblivionDrive.Application.VehicleGroupModule.DTOs;

namespace OblivionDrive.Application.VehicleGroupModule.commands;
public record UpdateVehicleGroupCommand(
    Guid VehicleGroupId, string name) : IRequest<Result<UpdatedVehicleGroupDTO>>;