using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OblivionDrive.Application.AuthenticationModule.Commands;
using OblivionDrive.Application.AuthenticationModule.DTOs;
using OblivionDrive.Application.AuthenticationModule.Extensions;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;

namespace OblivionDrive.Application.AuthenticationModule.Handlers;
public class RegisterUserHandler(   
    UserManager<User> userManager,
    IValidator<RegisterUserCommand> validator,
    ITokenProvider tokenProvider
) : IRequestHandler<RegisterUserCommand, Result<AccessToken>>
{
    public async Task<Result<AccessToken>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            List<string> validationErrors = validationResult.Errors
                .Select(e => e.ErrorMessage)
                .ToList();

            return Result.Fail(ErrorResults.InvalidRequestError(validationErrors));
        }

        User newUser = new User
        {
            UserName = command.UserName,
            Email = command.Email,
            UserType = UserType.Company,
        };

        newUser.CompanyId = newUser.Id;

        IdentityResult userResult = await userManager.CreateAsync(newUser, command.Password);

        if (!userResult.Succeeded)
            return userResult.ToInvalidRequestResult<AccessToken>();

        string roleName = newUser.UserType.ToString();

        IdentityResult roleResult = await userManager.AddToRoleAsync(newUser, roleName);

        if (!roleResult.Succeeded)
            return userResult.ToInvalidRequestResult<AccessToken>();


        var accessToken = tokenProvider.CreateAcessToken(newUser) as AccessToken;

        if (accessToken is null)
        {
            return Result.Fail(ErrorResults.InternalExceptionError(
                new Exception("Ocorreu um erro ao gerar token de acesso.")));
        }

        return Result.Ok(accessToken);
    }
}