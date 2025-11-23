using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OblivionDrive.Application.AuthenticationModule.Commands;
using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Application.AuthenticationModule.Handlers;
public class LogoutUserHandler(SignInManager<User> signInManager
) : IRequestHandler<LogoutUserCommand, Result>
{
    public async Task<Result> Handle(LogoutUserCommand command, CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();

        return Result.Ok();
    }
}