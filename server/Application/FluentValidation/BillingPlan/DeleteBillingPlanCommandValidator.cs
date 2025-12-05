using FluentValidation;
using OblivionDrive.Application.BillingPlanModule.Commands;

namespace OblivionDrive.Application.FluentValidation.BillingPlan;

public class DeleteBillingPlanCommandValidator : AbstractValidator<DeleteBillingPlanCommand>
{
    public DeleteBillingPlanCommandValidator()
    {
        RuleFor(c => c.BillingPlanId)
            .NotEmpty()
                .WithMessage("O identificador do plano de cobrança é obrigatório.");
    }
}