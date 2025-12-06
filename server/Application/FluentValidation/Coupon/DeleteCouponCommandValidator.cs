using FluentValidation;
using OblivionDrive.Application.CouponModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Coupon;

public class DeleteCouponCommandValidator : AbstractValidator<DeleteCouponCommand>
{
    public DeleteCouponCommandValidator()
    {
        RuleFor(c => c.CouponId)
            .NotEmpty()
                .WithMessage("O identificador do cupom é obrigatório.");
    }
}