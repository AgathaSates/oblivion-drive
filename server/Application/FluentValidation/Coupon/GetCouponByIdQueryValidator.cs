using FluentValidation;
using OblivionDrive.Application.CouponModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Coupon;
public class GetCouponByIdQueryValidator : AbstractValidator<GetCouponByIdQuery>
{
    public GetCouponByIdQueryValidator()
    {
        RuleFor(q => q.CouponId)
            .NotEmpty()
                .WithMessage("O identificador do cupom é obrigatório.");
    }
}