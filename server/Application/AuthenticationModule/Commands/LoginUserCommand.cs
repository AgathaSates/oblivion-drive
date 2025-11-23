using FluentResults;
using MediatR;
using OblivionDrive.Application.AuthenticationModule.DTOs;

namespace OblivionDrive.Application.AuthenticationModule.Commands;
public record LoginUserCommand(
    string UserName, 
    string Password
    ) : IRequest<Result<AccessToken>>;
