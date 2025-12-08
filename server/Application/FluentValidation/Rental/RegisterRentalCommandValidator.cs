using FluentValidation;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Application.FluentValidation.Rental;

public class RegisterRentalCommandValidator : AbstractValidator<RegisterRentalCommand>
{
    private readonly DateOnly _minimumDate = new(2000, 1, 1);

    public RegisterRentalCommandValidator()
    {
        RuleFor(c => c.ClientId)
            .NotEmpty()
                .WithMessage("O identificador do cliente é obrigatório.");

        RuleFor(c => c.DriverId)
            .NotEmpty()
                .WithMessage("O identificador do condutor é obrigatório.");

        RuleFor(c => c.VehicleId)
            .NotEmpty()
                .WithMessage("O identificador do veículo é obrigatório.");

        RuleFor(c => c.PlanType)
            .IsInEnum()
                .WithMessage("O tipo de plano selecionado é inválido.");

        RuleFor(c => c.StartDate)
            .GreaterThanOrEqualTo(_minimumDate)
                .WithMessage($"A data de saída não pode ser anterior a {_minimumDate:dd/MM/yyyy}.");

        RuleFor(c => c.ExpectedReturnDate)
            .GreaterThanOrEqualTo(c => c.StartDate)
                .WithMessage("A data prevista de retorno deve ser maior ou igual à data de saída.");

        RuleFor(c => c.InsuranceDailyPricePerPerson)
            .GreaterThanOrEqualTo(0m)
                .WithMessage("O valor diário do seguro por pessoa não pode ser negativo.");

        RuleFor(c => c.InsurancePersonsCount)
            .GreaterThanOrEqualTo(0)
                .WithMessage("A quantidade de pessoas para o seguro não pode ser negativa.");

        RuleFor(c => c.EstimatedTotalKilometers)
            .NotNull()
                .WithMessage("A quilometragem estimada é obrigatória para o Plano Controlado.")
            .GreaterThan(0)
                .WithMessage("A quilometragem estimada deve ser maior que zero para o Plano Controlado.")
            .When(c => c.PlanType == RentalPlanType.Controlled);
    }
}