using FluentValidation;
using OblivionDrive.Application.DriverModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Driver;
public class RegisterDriverCommandValidator : AbstractValidator<RegisterDriverCommand>
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    private const int MaximumEmailLength = 255;
    private const int MaximumPhoneLength = 20;

    private const int MaximumCpfLength = 14;
    private const int MaximumCnhLength = 20;

    private const string NamePattern = @"^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$";
    private const string DigitsOnlyPattern = @"^[0-9]+$";
    private const string CpfPattern = @"^[0-9]{11}$";

    public RegisterDriverCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do condutor é obrigatório.")
            .MinimumLength(MinimumNameLength)
                .WithMessage($"O nome do condutor deve ter pelo menos {MinimumNameLength} caracteres.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"O nome do condutor deve ter no máximo {MaximumNameLength} caracteres.")
            .Matches(NamePattern)
                .WithMessage("O nome do condutor deve conter apenas letras e espaços.");

        RuleFor(c => c.Email)
            .NotEmpty()
                .WithMessage("O email do condutor é obrigatório.")
            .MaximumLength(MaximumEmailLength)
                .WithMessage($"O email do condutor deve ter no máximo {MaximumEmailLength} caracteres.")
            .EmailAddress()
                .WithMessage("O email do condutor deve ser válido.");

        RuleFor(c => c.PhoneNumber)
            .NotEmpty()
                .WithMessage("O telefone do condutor é obrigatório.")
            .MaximumLength(MaximumPhoneLength)
                .WithMessage($"O telefone do condutor deve ter no máximo {MaximumPhoneLength} caracteres.")
            .Matches(DigitsOnlyPattern)
                .WithMessage("O telefone do condutor deve conter apenas números.");

        RuleFor(c => c.Cpf)
            .NotEmpty()
                .WithMessage("O CPF do condutor é obrigatório.")
            .MaximumLength(MaximumCpfLength)
                .WithMessage($"O CPF do condutor deve ter no máximo {MaximumCpfLength} caracteres.")
            .Matches(CpfPattern)
                .WithMessage("O CPF do condutor deve conter exatamente 11 dígitos numéricos.");

        RuleFor(c => c.Cnh)
            .NotEmpty()
                .WithMessage("A CNH do condutor é obrigatória.")
            .MaximumLength(MaximumCnhLength)
                .WithMessage($"A CNH do condutor deve ter no máximo {MaximumCnhLength} caracteres.")
            .Matches(DigitsOnlyPattern)
                .WithMessage("A CNH do condutor deve conter apenas números.");

        RuleFor(c => c.CnhExpirationDate)
            .Must(date => date >= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("A validade da CNH do condutor deve ser maior ou igual à data atual.");

        RuleFor(c => c.ClientId)
            .NotEmpty()
                .WithMessage("O identificador do cliente vinculado ao condutor é obrigatório.");
    }
}