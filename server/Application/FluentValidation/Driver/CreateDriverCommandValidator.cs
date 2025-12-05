using FluentValidation;
using OblivionDrive.Application.DriverModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Driver;
public class CreateDriverCommandValidator : AbstractValidator<CreateDriverCommand>
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    private const int MaximumEmailLength = 255;
    private const int MaximumPhoneLength = 20;

    private const int MaximumCpfLength = 14;
    private const int MaximumCnhLength = 20;

    public CreateDriverCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do condutor é obrigatório.")
            .MinimumLength(MinimumNameLength)
                .WithMessage($"O nome do condutor deve ter pelo menos {MinimumNameLength} caracteres.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"O nome do condutor deve ter no máximo {MaximumNameLength} caracteres.");

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
                .WithMessage($"O telefone do condutor deve ter no máximo {MaximumPhoneLength} caracteres.");

        RuleFor(c => c.Cpf)
            .NotEmpty()
                .WithMessage("O CPF do condutor é obrigatório.")
            .MaximumLength(MaximumCpfLength)
                .WithMessage($"O CPF do condutor deve ter no máximo {MaximumCpfLength} caracteres.");

        RuleFor(c => c.Cnh)
            .NotEmpty()
                .WithMessage("A CNH do condutor é obrigatória.")
            .MaximumLength(MaximumCnhLength)
                .WithMessage($"A CNH do condutor deve ter no máximo {MaximumCnhLength} caracteres.");

        RuleFor(c => c.CnhExpirationDate)
            .Must(date => date >= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("A validade da CNH do condutor deve ser maior ou igual à data atual.");

        RuleFor(c => c.ClientId)
            .NotEmpty()
                .WithMessage("O identificador do cliente vinculado ao condutor é obrigatório.");
    }
}