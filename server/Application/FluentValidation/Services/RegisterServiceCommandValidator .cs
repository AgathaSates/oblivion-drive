using FluentValidation;
using OblivionDrive.Application.ServicesModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Services;
public class RegisterServiceCommandValidator : AbstractValidator<RegisterServiceCommand>
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;
    private const decimal MaximumPrice = 1_000_000m;

    public RegisterServiceCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do serviço é obrigatório.")
            .MinimumLength(MinimumNameLength)
                .WithMessage($"O nome do serviço deve ter pelo menos {MinimumNameLength} caracteres.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"O nome do serviço deve ter no máximo {MaximumNameLength} caracteres.");

        RuleFor(c => c.Price)
            .GreaterThan(0)
                .WithMessage("O preço do serviço deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumPrice)
                .WithMessage($"O preço do serviço não pode ser maior que {MaximumPrice:N2}.");

        RuleFor(c => c.ChargeType)
            .IsInEnum()
                .WithMessage("O tipo de cobrança informado é inválido.");
    }
}