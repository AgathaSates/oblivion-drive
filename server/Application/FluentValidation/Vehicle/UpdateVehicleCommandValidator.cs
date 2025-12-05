using FluentValidation;
using OblivionDrive.Application.VehicleModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Vehicle;
public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    private const int MinimumBrandLength = 2;
    private const int MaximumBrandLength = 200;

    private const int MinimumModelLength = 1;
    private const int MaximumModelLength = 200;

    private const int MinimumColorLength = 1;
    private const int MaximumColorLength = 100;

    private const decimal MinimumFuelTankCapacity = 0.1m;
    private const decimal MaximumFuelTankCapacity = 1_000m;

    private const int MinimumYear = 1900;
    private static int MaximumYear => DateTime.UtcNow.Year + 1;

    public UpdateVehicleCommandValidator()
    {
        RuleFor(c => c.VehicleId)
            .NotEmpty()
                .WithMessage("O identificador do veículo é obrigatório.");

        RuleFor(c => c.Brand)
            .NotEmpty()
                .WithMessage("A marca do veículo é obrigatória.")
            .MinimumLength(MinimumBrandLength)
                .WithMessage($"A marca do veículo deve ter pelo menos {MinimumBrandLength} caracteres.")
            .MaximumLength(MaximumBrandLength)
                .WithMessage($"A marca do veículo deve ter no máximo {MaximumBrandLength} caracteres.");

        RuleFor(c => c.Model)
            .NotEmpty()
                .WithMessage("O modelo do veículo é obrigatório.")
            .MinimumLength(MinimumModelLength)
                .WithMessage($"O modelo do veículo deve ter pelo menos {MinimumModelLength} caractere(s).")
            .MaximumLength(MaximumModelLength)
                .WithMessage($"O modelo do veículo deve ter no máximo {MaximumModelLength} caracteres.");

        RuleFor(c => c.Color)
            .NotEmpty()
                .WithMessage("A cor do veículo é obrigatória.")
            .MinimumLength(MinimumColorLength)
                .WithMessage($"A cor do veículo deve ter pelo menos {MinimumColorLength} caractere(s).")
            .MaximumLength(MaximumColorLength)
                .WithMessage($"A cor do veículo deve ter no máximo {MaximumColorLength} caracteres.");

        RuleFor(c => c.FuelTankCapacityInLiters)
            .GreaterThanOrEqualTo(MinimumFuelTankCapacity)
                .WithMessage($"A capacidade do tanque deve ser maior ou igual a {MinimumFuelTankCapacity}.")
            .LessThanOrEqualTo(MaximumFuelTankCapacity)
                .WithMessage($"A capacidade do tanque não pode ser maior que {MaximumFuelTankCapacity} litros.");

        RuleFor(c => c.Year)
            .InclusiveBetween(MinimumYear, MaximumYear)
                .WithMessage($"O ano do veículo deve estar entre {MinimumYear} e {MaximumYear}.");

        RuleFor(c => c.VehicleGroupId)
            .NotEmpty()
                .WithMessage("O identificador do grupo de veículos é obrigatório.");

        RuleFor(c => c.PhotoBytes)
            .Must(bytes => bytes is null || bytes.Length > 0)
                .WithMessage("A foto do veículo, se informada, não pode estar vazia.");
    }
}