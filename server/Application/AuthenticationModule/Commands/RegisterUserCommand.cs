using FluentResults;
using MediatR;
using OblivionDrive.Application.AuthenticationModule.DTOs;

namespace OblivionDrive.Application.AuthenticationModule.Commands;
public record RegisterUserCommand(
    string UserName,
    string Email,
    string Password
    ) : IRequest<Result<AccessToken>>;
