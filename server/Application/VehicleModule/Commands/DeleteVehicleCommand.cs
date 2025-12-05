using FluentResults;
using MediatR;

namespace OblivionDrive.Application.VehicleModule.Commands;
public record DeleteVehicleCommand(Guid VehicleId) : IRequest<Result>;