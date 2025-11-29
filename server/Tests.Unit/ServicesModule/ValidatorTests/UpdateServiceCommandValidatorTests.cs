using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Services;
using OblivionDrive.Application.ServicesModule.Commands;
using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Tests.Unit.ServicesModule.ValidatorTests;

[TestClass]
[TestCategory("Service - UpdateServiceCommandValidator Unit Tests")]
public class UpdateServiceCommandValidatorTests
{
    private UpdateServiceCommandValidator _validator = null!;

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;
    private const decimal MaximumPrice = 1_000_000m;

    private readonly string _serviceIdRequiredMessage =
        "O identificador do serviço é obrigatório.";

    private readonly string _nameRequiredMessage =
        "O nome do serviço é obrigatório.";

    private readonly string _nameMinMessage =
        $"O nome do serviço deve ter pelo menos {MinimumNameLength} caracteres.";

    private readonly string _nameMaxMessage =
        $"O nome do serviço deve ter no máximo {MaximumNameLength} caracteres.";

    private readonly string _priceGreaterThanZeroMessage =
        "O preço do serviço deve ser maior que zero.";

    private readonly string _priceMaxMessage =
        $"O preço do serviço não pode ser maior que {MaximumPrice:N2}.";

    private readonly string _chargeTypeInvalidMessage =
        "O tipo de cobrança informado é inválido.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateServiceCommandValidator();
    }

    private static UpdateServiceCommand CreateValidCommand()
    {
        return new UpdateServiceCommand(
            ServiceId: Guid.NewGuid(),
            Name: "Valid service",
            Price: 100m,
            ChargeType: (ChargeType)1
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        var command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }


    [TestMethod]
    public void Should_Fail_When_ServiceId_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { ServiceId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateServiceCommand.ServiceId) &&
            e.ErrorMessage == _serviceIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { Name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateServiceCommand.Name) &&
            e.ErrorMessage == _nameRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        var command = CreateValidCommand() with { Name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateServiceCommand.Name) &&
            e.ErrorMessage == _nameMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        var longName = new string('A', MaximumNameLength + 1);
        var command = CreateValidCommand() with { Name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateServiceCommand.Name) &&
            e.ErrorMessage == _nameMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Price_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        var command = CreateValidCommand() with { Price = 0m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateServiceCommand.Price) &&
            e.ErrorMessage == _priceGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Price_Is_Greater_Than_Maximum()
    {
        // arrange
        var command = CreateValidCommand() with { Price = MaximumPrice + 0.01m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateServiceCommand.Price) &&
            e.ErrorMessage == _priceMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ChargeType_Is_Invalid()
    {
        // arrange
        var command = CreateValidCommand() with { ChargeType = (ChargeType)999 };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateServiceCommand.ChargeType) &&
            e.ErrorMessage == _chargeTypeInvalidMessage));
    }
}