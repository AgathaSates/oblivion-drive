using FluentValidation;
using OblivionDrive.Application.PartnerModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Partner;
public class RegisterPartnerCommandValidator : AbstractValidator<RegisterPartnerCommand>
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    private const string NamePattern = @"^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$";

    public RegisterPartnerCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do parceiro é obrigatório.")
            .MinimumLength(MinimumNameLength)
                .WithMessage($"O nome do parceiro deve ter pelo menos {MinimumNameLength} caracteres.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"O nome do parceiro deve ter no máximo {MaximumNameLength} caracteres.")
            .Matches(NamePattern)
                .WithMessage("O nome do parceiro deve conter apenas letras e espaços.");
    }
}