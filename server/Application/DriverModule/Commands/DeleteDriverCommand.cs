using FluentResults;
using MediatR;

namespace OblivionDrive.Application.DriverModule.Commands;
public record DeleteDriverCommand(Guid DriverId) : IRequest<Result>;