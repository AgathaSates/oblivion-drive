using FluentValidation;
using OblivionDrive.Application.CouponModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Coupon;
public class GetAllCouponsQueryValidator : AbstractValidator<GetAllCouponsQuery>
{
    private const int MaximumQuantity = 1_000;

    public GetAllCouponsQueryValidator()
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