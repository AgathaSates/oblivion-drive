using FluentResults;
using MediatR;

namespace OblivionDrive.Application.AuthenticationModule.Commands;
public record LogoutUserCommand : IRequest<Result>;
