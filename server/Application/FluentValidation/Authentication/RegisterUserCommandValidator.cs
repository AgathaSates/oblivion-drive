using FluentValidation;
using OblivionDrive.Application.AuthenticationModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Authentication;
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private const int MinimumPasswordLength = 6;
    private const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    private const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$";
    private const string UsernameRegex = @"^\S+$";

    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.UserName)
            .NotEmpty()
                .WithMessage("O nome de usuário é obrigatório.")
            .MinimumLength(3)
                .WithMessage("O nome de usuário deve ter pelo menos 3 caracteres.")
            .MaximumLength(100)
                .WithMessage("O nome de usuário deve ter no máximo 100 caracteres.")
            .Matches(UsernameRegex)
                .WithMessage("O nome de usuário não deve conter espaços em branco.");

        RuleFor(c => c.Email)
             .NotEmpty()
                .WithMessage("O e-mail é obrigatório.")
             .MaximumLength(256)
                .WithMessage("O e-mail deve ter no máximo 256 caracteres.")
             .Matches(EmailRegex)
                 .WithMessage("O e-mail deve estar no formato [ nome@dominio.com ].");

        RuleFor(c => c.Password)
            .NotEmpty()
                .WithMessage("A senha é obrigatória.")
            .MinimumLength(MinimumPasswordLength)
                .WithMessage($"A senha deve ter pelo menos {MinimumPasswordLength} caracteres.")
            .MaximumLength(100)
                .WithMessage("A senha deve ter no máximo 100 caracteres.")
            .Matches(PasswordRegex)
                .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma letra minúscula, um número e um caractere especial.");
    }
}