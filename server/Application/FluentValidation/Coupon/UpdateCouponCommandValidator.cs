using FluentValidation;
using OblivionDrive.Application.CouponModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Coupon;
public class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 100;

    private const decimal MaximumCouponValue = 1_000_000m;

    private const string NamePattern = @"^[A-Z0-9]+$";

    public UpdateCouponCommandValidator()
    {
        RuleFor(c => c.CouponId)
            .NotEmpty()
                .WithMessage("O identificador do cupom é obrigatório.");

        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do cupom é obrigatório.")
            .MinimumLength(MinimumNameLength)
                .WithMessage($"O nome do cupom deve ter pelo menos {MinimumNameLength} caracteres.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"O nome do cupom deve ter no máximo {MaximumNameLength} caracteres.")
            .Matches(NamePattern)
                .WithMessage("O nome do cupom deve conter apenas letras maiúsculas e números, sem espaços.");

        RuleFor(c => c.Value)
            .GreaterThan(0)
                .WithMessage("O valor do cupom deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumCouponValue)
                .WithMessage($"O valor do cupom não pode ser maior que {MaximumCouponValue:N2}.");

        RuleFor(c => c.ExpirationDate)
            .Must(date => date >= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("A data de validade do cupom deve ser maior ou igual à data atual.");

        RuleFor(c => c.PartnerId)
            .NotEmpty()
                .WithMessage("O identificador do parceiro vinculado ao cupom é obrigatório.");
    }
}