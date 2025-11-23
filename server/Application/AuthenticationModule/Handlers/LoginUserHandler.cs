using System.ComponentModel.DataAnnotations;
using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OblivionDrive.Application.AuthenticationModule.Commands;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Application.AuthenticationModule.Handlers;
public class LoginUserHandler(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    IValidator<LoginUserCommand> validator,
    ITokenProvider tokenProvider
) : IRequestHandler<LoginUserCommand, Result<AccessToken>>
{
    public async Task<Result<AccessToken>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            List<string> validationErrors = validationResult.Errors
                .Select(e => e.ErrorMessage)
                .ToList();

            return Result.Fail(ErrorResults.InvalidRequestError(validationErrors));
        }

        var user = await userManager.FindByNameAsync(command.UserName);

        if (user == null)
        {
            return Result.Fail(
                ErrorResults.UserNotFoundError(command.UserName));
        }

        var loginResult = await signInManager.PasswordSignInAsync(
             command.UserName,
             command.Password,
             isPersistent: false,
             lockoutOnFailure: true
        );        

        if (!loginResult.Succeeded)
            return Result.Fail(ErrorResults.IncorrectCredentialsError());

        var accessToken = tokenProvider.CreateAcessToken(user) as AccessToken;

        if (accessToken == null)
            return Result.Fail(ErrorResults.InternalExceptionError(new Exception("Falha ao gerar token de acesso")));

        return Result.Ok(accessToken);
    }
}