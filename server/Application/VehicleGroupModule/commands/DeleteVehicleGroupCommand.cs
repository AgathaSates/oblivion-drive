using FluentResults;
using MediatR;

namespace OblivionDrive.Application.VehicleGroupModule.commands;
public record DeleteVehicleGroupCommand(Guid VehicleGroupId) : IRequest<Result>;