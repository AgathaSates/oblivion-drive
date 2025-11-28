using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.FuelPriceConfiguration;
using OblivionDrive.Application.FuelPriceConfigurationModule.Commands;

namespace OblivionDrive.Tests.Unit.FuelPriceConfigurationModule.ValidatorTests;

[TestClass]
[TestCategory("FuelPriceConfiguration - UpdateFuelPriceConfigurationCommandValidator Unit Tests")]
public class UpdateFuelPriceConfigurationCommandValidatorTests
{
    private UpdateFuelPriceConfigurationCommandValidator _validator = null!;

    private const decimal MinimumFuelPrice = 0.01m;

    private readonly string _gasolineMinMessage =
        $"O preço da gasolina deve ser maior que zero e não pode ser menor que {MinimumFuelPrice:0.00}.";

    private readonly string _gasMinMessage =
        $"O preço do gás deve ser maior que zero e não pode ser menor que {MinimumFuelPrice:0.00}.";

    private readonly string _dieselMinMessage =
        $"O preço do diesel deve ser maior que zero e não pode ser menor que {MinimumFuelPrice:0.00}.";

    private readonly string _alcoholMinMessage =
        $"O preço do álcool deve ser maior que zero e não pode ser menor que {MinimumFuelPrice:0.00}.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateFuelPriceConfigurationCommandValidator();
    }

    [TestMethod]
    public void Should_Pass_When_All_Prices_Are_Valid()
    {
        // arrange
        var command = new UpdateFuelPriceConfigurationCommand(
            Gasoline: 5.79m,
            Gas: 4.10m,
            Diesel: 6.20m,
            Alcohol: 3.99m);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_Gasoline_Is_Less_Than_Minimum()
    {
        // arrange
        var command = new UpdateFuelPriceConfigurationCommand(
            Gasoline: 0.00m,
            Gas: 4.10m,
            Diesel: 6.20m,
            Alcohol: 3.99m);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateFuelPriceConfigurationCommand.Gasoline) &&
            error.ErrorMessage == _gasolineMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Gas_Is_Less_Than_Minimum()
    {
        // arrange
        var command = new UpdateFuelPriceConfigurationCommand(
            Gasoline: 5.79m,
            Gas: 0.00m,
            Diesel: 6.20m,
            Alcohol: 3.99m);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateFuelPriceConfigurationCommand.Gas) &&
            error.ErrorMessage == _gasMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Diesel_Is_Less_Than_Minimum()
    {
        // arrange
        var command = new UpdateFuelPriceConfigurationCommand(
            Gasoline: 5.79m,
            Gas: 4.10m,
            Diesel: 0.00m,
            Alcohol: 3.99m);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateFuelPriceConfigurationCommand.Diesel) &&
            error.ErrorMessage == _dieselMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Alcohol_Is_Less_Than_Minimum()
    {
        // arrange
        var command = new UpdateFuelPriceConfigurationCommand(
            Gasoline: 5.79m,
            Gas: 4.10m,
            Diesel: 6.20m,
            Alcohol: 0.00m);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateFuelPriceConfigurationCommand.Alcohol) &&
            error.ErrorMessage == _alcoholMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Gasoline_Has_More_Than_Two_Decimal_Places()
    {
        // arrange
        var command = new UpdateFuelPriceConfigurationCommand(
            Gasoline: 5.799m,
            Gas: 4.10m,
            Diesel: 6.20m,
            Alcohol: 3.99m);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateFuelPriceConfigurationCommand.Gasoline) &&
            error.ErrorMessage == "O preço da gasolina deve ter no máximo duas casas decimais."));
    }
}