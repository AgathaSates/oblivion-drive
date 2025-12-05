using FluentValidation;
using OblivionDrive.Application.BillingPlanModule.Querys;

namespace OblivionDrive.Application.FluentValidation.BillingPlan;
public class GetAllBillingPlanQueryValidator : AbstractValidator<GetAllBillingPlanQuery>
{
    private const int MaximumQuantity = 1_000;

    public GetAllBillingPlanQueryValidator()
    {
        RuleFor(q => q.Quantity)
            .GreaterThan(0)
                .When(q => q.Quantity.HasValue)
                .WithMessage("A quantidade deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumQuantity)
                .When(q => q.Quantity.HasValue)
                .WithMessage($"A quantidade não pode ser maior que {MaximumQuantity}.");
    }
}