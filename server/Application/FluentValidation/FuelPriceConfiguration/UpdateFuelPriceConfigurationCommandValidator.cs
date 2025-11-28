using FluentValidation;
using OblivionDrive.Application.FuelPriceConfigurationModule.Commands;

namespace OblivionDrive.Application.FluentValidation.FuelPriceConfiguration;
public class UpdateFuelPriceConfigurationCommandValidator : AbstractValidator<UpdateFuelPriceConfigurationCommand>
{
    private const decimal MinimumFuelPrice = 0.01m;

    public UpdateFuelPriceConfigurationCommandValidator()
    {
        RuleFor(command => command.Gasoline)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(MinimumFuelPrice)
                .WithMessage($"O preço da gasolina deve ser maior que zero e não pode ser menor que {MinimumFuelPrice:0.00}.")
            .Must(HaveAtMostTwoDecimalPlaces)
                .WithMessage("O preço da gasolina deve ter no máximo duas casas decimais.");

        RuleFor(command => command.Gas)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(MinimumFuelPrice)
                .WithMessage($"O preço do gás deve ser maior que zero e não pode ser menor que {MinimumFuelPrice:0.00}.")
            .Must(HaveAtMostTwoDecimalPlaces)
                .WithMessage("O preço do gás deve ter no máximo duas casas decimais.");

        RuleFor(command => command.Diesel)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(MinimumFuelPrice)
                .WithMessage($"O preço do diesel deve ser maior que zero e não pode ser menor que {MinimumFuelPrice:0.00}.")
            .Must(HaveAtMostTwoDecimalPlaces)
                .WithMessage("O preço do diesel deve ter no máximo duas casas decimais.");

        RuleFor(command => command.Alcohol)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(MinimumFuelPrice)
                .WithMessage($"O preço do álcool deve ser maior que zero e não pode ser menor que {MinimumFuelPrice:0.00}.")
            .Must(HaveAtMostTwoDecimalPlaces)
                .WithMessage("O preço do álcool deve ter no máximo duas casas decimais.");
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal value)
    {
        var roundedValue = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        return roundedValue == value;
    }
}