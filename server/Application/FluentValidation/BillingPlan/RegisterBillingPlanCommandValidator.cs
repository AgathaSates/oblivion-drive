using FluentValidation;
using OblivionDrive.Application.BillingPlanModule.Commands;

namespace OblivionDrive.Application.FluentValidation.BillingPlan;
public class RegisterBillingPlanCommandValidator : AbstractValidator<RegisterBillingPlanCommand>
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;
    private const decimal MaximumRate = 1_000_000m;

    public RegisterBillingPlanCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do plano de cobrança é obrigatório.")
            .MinimumLength(MinimumNameLength)
                .WithMessage($"O nome do plano de cobrança deve ter pelo menos {MinimumNameLength} caracteres.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"O nome do plano de cobrança deve ter no máximo {MaximumNameLength} caracteres.");

        RuleFor(c => c.VehicleGroupId)
            .NotEmpty()
                .WithMessage("O identificador do grupo de veículos é obrigatório.");

        RuleFor(c => c.DailyPlanDailyRate)
            .GreaterThan(0)
                .WithMessage("A diária do plano diário deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumRate)
                .WithMessage($"A diária do plano diário não pode ser maior que {MaximumRate:N2}.");

        RuleFor(c => c.DailyPlanPricePerKilometer)
            .GreaterThanOrEqualTo(0)
                .WithMessage("O preço por KM do plano diário não pode ser negativo.")
            .LessThanOrEqualTo(MaximumRate)
                .WithMessage($"O preço por KM do plano diário não pode ser maior que {MaximumRate:N2}.");

        RuleFor(c => c.ControlledPlanDailyRate)
            .GreaterThan(0)
                .WithMessage("A diária do plano controlado deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumRate)
                .WithMessage($"A diária do plano controlado não pode ser maior que {MaximumRate:N2}.");

        RuleFor(c => c.ControlledPlanExtraPricePerKilometer)
            .GreaterThanOrEqualTo(0)
                .WithMessage("O preço extra por KM do plano controlado não pode ser negativo.")
            .LessThanOrEqualTo(MaximumRate)
                .WithMessage($"O preço extra por KM do plano controlado não pode ser maior que {MaximumRate:N2}.");

        RuleFor(c => c.FreePlanDailyRate)
            .GreaterThan(0)
                .WithMessage("A diária do plano livre deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumRate)
                .WithMessage($"A diária do plano livre não pode ser maior que {MaximumRate:N2}.");
    }
}