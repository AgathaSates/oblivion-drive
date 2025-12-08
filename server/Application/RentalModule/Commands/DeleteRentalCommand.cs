using FluentResults;
using MediatR;

namespace OblivionDrive.Application.RentalModule.Commands;
public record DeleteRentalCommand(Guid RentalId) : IRequest<Result>;