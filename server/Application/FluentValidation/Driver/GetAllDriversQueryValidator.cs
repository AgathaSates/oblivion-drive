using FluentValidation;
using OblivionDrive.Application.DriverModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Driver;
public class GetAllDriversQueryValidator : AbstractValidator<GetAllDriversQuery>
{
    private const int MaximumQuantity = 1_000;

    public GetAllDriversQueryValidator()
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