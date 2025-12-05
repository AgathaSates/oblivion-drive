using FluentResults;
using MediatR;

namespace OblivionDrive.Application.ClientModule.Commands;
public record DeleteClientCommand(Guid ClientId) : IRequest<Result>;