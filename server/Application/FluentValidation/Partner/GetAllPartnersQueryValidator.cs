using FluentValidation;
using OblivionDrive.Application.PartnerModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Partner;
public class GetAllPartnersQueryValidator : AbstractValidator<GetAllPartnersQuery>
{
    private const int MaximumQuantity = 1_000;

    public GetAllPartnersQueryValidator()
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