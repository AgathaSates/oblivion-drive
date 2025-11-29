using FluentResults;
using MediatR;

namespace OblivionDrive.Application.ServicesModule.Commands;
public record DeleteServiceCommand(Guid ServiceId) : IRequest<Result>;