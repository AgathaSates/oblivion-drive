using FluentValidation;
using OblivionDrive.Application.BillingPlanModule.Querys;

namespace OblivionDrive.Application.FluentValidation.BillingPlan;
public class GetBillingPlanByIdQueryValidator : AbstractValidator<GetBillingPlanByIdQuery>
{
    public GetBillingPlanByIdQueryValidator()
    {
        RuleFor(q => q.BillingPlanId)
            .NotEmpty()
                .WithMessage("O identificador do plano de cobrança é obrigatório.");
    }
}