using FluentValidation;
using OblivionDrive.Application.RentalModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Rental;

public class CompleteRentalReturnCommandValidator : AbstractValidator<CompleteRentalReturnCommand>
{
    private readonly DateOnly _minimumDate = new(2000, 1, 1);

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 100;
    private const string NamePattern = @"^[A-Z0-9]+$";

    public CompleteRentalReturnCommandValidator()
    {
        RuleFor(c => c.RentalId)
            .NotEmpty()
                .WithMessage("O identificador do aluguel é obrigatório.");

        RuleFor(c => c.ActualReturnDate)
            .GreaterThanOrEqualTo(_minimumDate)
                .WithMessage($"A data de devolução não pode ser anterior a {_minimumDate:dd/MM/yyyy}.");

        RuleFor(c => c.InitialOdometerInKm)
            .GreaterThanOrEqualTo(0)
                .WithMessage("A quilometragem inicial não pode ser negativa.");

        RuleFor(c => c.CurrentOdometerInKm)
            .GreaterThanOrEqualTo(0)
                .WithMessage("A quilometragem atual não pode ser negativa.")
            .GreaterThanOrEqualTo(c => c.InitialOdometerInKm)
                .WithMessage("A quilometragem atual deve ser maior ou igual à quilometragem inicial.");

        When(c => !string.IsNullOrWhiteSpace(c.CouponName), () =>
        {
            RuleFor(c => c.CouponName!)
                .MinimumLength(MinimumNameLength)
                    .WithMessage($"O nome do cupom deve ter pelo menos {MinimumNameLength} caracteres.")
                .MaximumLength(MaximumNameLength)
                    .WithMessage($"O nome do cupom deve ter no máximo {MaximumNameLength} caracteres.")
                .Matches(NamePattern)
                    .WithMessage("O nome do cupom deve conter apenas letras maiúsculas e números, sem espaços.");
        });
    }
}
