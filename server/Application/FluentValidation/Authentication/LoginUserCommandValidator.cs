using FluentValidation;
using OblivionDrive.Application.AuthenticationModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Authentication;
public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    private const int MinimumPasswordLength = 6;
    private const string PasswordRegex =
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$";

    public LoginUserCommandValidator() 
    {
        RuleFor(c => c.UserName)
            .NotEmpty()
                .WithMessage("O nome de usuário é obrigatório.")
             .MinimumLength(3)
                .WithMessage("O nome de usuário deve ter pelo menos 3 caracteres.");

        RuleFor(c => c.Password)
            .NotEmpty()
                .WithMessage("A senha é obrigatória.")
            .MinimumLength(MinimumPasswordLength)
                .WithMessage($"A senha deve ter pelo menos {MinimumPasswordLength} caracteres.")
            .Matches(PasswordRegex)
                .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma letra minúscula, um número e um caractere especial.");
    }
}
