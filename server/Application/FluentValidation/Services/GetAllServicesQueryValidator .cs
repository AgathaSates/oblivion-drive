using FluentValidation;
using OblivionDrive.Application.ServicesModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Services;
public sealed class GetAllServicesQueryValidator : AbstractValidator<GetAllServicesQuery>
{
    private const int MaximumQuantity = 1_000;

    public GetAllServicesQueryValidator()
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