using FluentResults;
using Microsoft.AspNetCore.Identity;
using OblivionDrive.Application.Shared;

namespace OblivionDrive.Application.AuthenticationModule.Extensions;
public static class IdentityResultExtensions
{
    public static Result<T> ToInvalidRequestResult<T>(this IdentityResult identityResult)
    {
        List<string> errors = identityResult.Errors.Select(err =>
        {
            return err.Code switch
            {
                "DuplicateUserName" => "Já existe um usuário com esse nome.",
                "DuplicateEmail" => "Já existe um usuário com esse e-mail.",
                "PasswordTooShort" => "A senha é muito curta.",
                "PasswordRequiresNonAlphanumeric" => "A senha deve conter pelo menos um caractere especial.",
                "PasswordRequiresDigit" => "A senha deve conter pelo menos um número.",
                "PasswordRequiresUpper" => "A senha deve conter pelo menos uma letra maiúscula.",
                "PasswordRequiresLower" => "A senha deve conter pelo menos uma letra minúscula.",
                _ => err.Description
            };
        }).ToList();

        return Result.Fail<T>(ErrorResults.InvalidRequestError(errors));
    }
}