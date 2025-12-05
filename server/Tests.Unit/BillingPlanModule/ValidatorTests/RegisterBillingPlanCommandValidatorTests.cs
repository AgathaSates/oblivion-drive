using FluentValidation.Results;
using OblivionDrive.Application.BillingPlanModule.Commands;
using OblivionDrive.Application.FluentValidation.BillingPlan;

namespace OblivionDrive.Tests.Unit.BillingPlanModule.ValidatorTests;

[TestClass]
[TestCategory("BillingPlan - RegisterBillingPlanCommandValidator Unit Tests")]
public class RegisterBillingPlanCommandValidatorTests
{
    private RegisterBillingPlanCommandValidator _validator = null!;

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;
    private const decimal MaximumRate = 1_000_000m;

    private readonly string _nameRequiredMessage =
        "O nome do plano de cobrança é obrigatório.";

    private readonly string _nameMinMessage =
        $"O nome do plano de cobrança deve ter pelo menos {MinimumNameLength} caracteres.";

    private readonly string _nameMaxMessage =
        $"O nome do plano de cobrança deve ter no máximo {MaximumNameLength} caracteres.";

    private readonly string _vehicleGroupIdRequiredMessage =
        "O identificador do grupo de veículos é obrigatório.";

    private readonly string _dailyPlanDailyRateGreaterThanZeroMessage =
        "A diária do plano diário deve ser maior que zero.";

    private readonly string _dailyPlanDailyRateMaxMessage =
        $"A diária do plano diário não pode ser maior que {MaximumRate:N2}.";

    private readonly string _dailyPlanPricePerKilometerNonNegativeMessage =
        "O preço por KM do plano diário não pode ser negativo.";

    private readonly string _dailyPlanPricePerKilometerMaxMessage =
        $"O preço por KM do plano diário não pode ser maior que {MaximumRate:N2}.";

    private readonly string _controlledPlanDailyRateGreaterThanZeroMessage =
        "A diária do plano controlado deve ser maior que zero.";

    private readonly string _controlledPlanDailyRateMaxMessage =
        $"A diária do plano controlado não pode ser maior que {MaximumRate:N2}.";

    private readonly string _controlledPlanExtraPricePerKilometerNonNegativeMessage =
        "O preço extra por KM do plano controlado não pode ser negativo.";

    private readonly string _controlledPlanExtraPricePerKilometerMaxMessage =
        $"O preço extra por KM do plano controlado não pode ser maior que {MaximumRate:N2}.";

    private readonly string _freePlanDailyRateGreaterThanZeroMessage =
        "A diária do plano livre deve ser maior que zero.";

    private readonly string _freePlanDailyRateMaxMessage =
        $"A diária do plano livre não pode ser maior que {MaximumRate:N2}.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new RegisterBillingPlanCommandValidator();
    }

    private static RegisterBillingPlanCommand CreateValidCommand()
    {
        return new RegisterBillingPlanCommand(
            Name: "Plano válido",
            VehicleGroupId: Guid.NewGuid(),
            DailyPlanDailyRate: 100m,
            DailyPlanPricePerKilometer: 2m,
            ControlledPlanDailyRate: 80m,
            ControlledPlanExtraPricePerKilometer: 3m,
            FreePlanDailyRate: 200m
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Empty()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with { Name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.Name) &&
            e.ErrorMessage == _nameRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with { Name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.Name) &&
            e.ErrorMessage == _nameMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        string longName = new('A', MaximumNameLength + 1);
        RegisterBillingPlanCommand command = CreateValidCommand() with { Name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.Name) &&
            e.ErrorMessage == _nameMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_VehicleGroupId_Is_Empty()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with { VehicleGroupId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.VehicleGroupId) &&
            e.ErrorMessage == _vehicleGroupIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_DailyPlanDailyRate_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with { DailyPlanDailyRate = 0m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.DailyPlanDailyRate) &&
            e.ErrorMessage == _dailyPlanDailyRateGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_DailyPlanDailyRate_Is_Greater_Than_Maximum()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with
        {
            DailyPlanDailyRate = MaximumRate + 0.01m
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.DailyPlanDailyRate) &&
            e.ErrorMessage == _dailyPlanDailyRateMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_DailyPlanPricePerKilometer_Is_Negative()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with { DailyPlanPricePerKilometer = -1m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.DailyPlanPricePerKilometer) &&
            e.ErrorMessage == _dailyPlanPricePerKilometerNonNegativeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_DailyPlanPricePerKilometer_Is_Greater_Than_Maximum()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with
        {
            DailyPlanPricePerKilometer = MaximumRate + 0.01m
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.DailyPlanPricePerKilometer) &&
            e.ErrorMessage == _dailyPlanPricePerKilometerMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ControlledPlanDailyRate_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with { ControlledPlanDailyRate = 0m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.ControlledPlanDailyRate) &&
            e.ErrorMessage == _controlledPlanDailyRateGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ControlledPlanDailyRate_Is_Greater_Than_Maximum()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with
        {
            ControlledPlanDailyRate = MaximumRate + 0.01m
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.ControlledPlanDailyRate) &&
            e.ErrorMessage == _controlledPlanDailyRateMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ControlledPlanExtraPricePerKilometer_Is_Negative()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with
        {
            ControlledPlanExtraPricePerKilometer = -1m
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.ControlledPlanExtraPricePerKilometer) &&
            e.ErrorMessage == _controlledPlanExtraPricePerKilometerNonNegativeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ControlledPlanExtraPricePerKilometer_Is_Greater_Than_Maximum()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with
        {
            ControlledPlanExtraPricePerKilometer = MaximumRate + 0.01m
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.ControlledPlanExtraPricePerKilometer) &&
            e.ErrorMessage == _controlledPlanExtraPricePerKilometerMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_FreePlanDailyRate_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with { FreePlanDailyRate = 0m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.FreePlanDailyRate) &&
            e.ErrorMessage == _freePlanDailyRateGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_FreePlanDailyRate_Is_Greater_Than_Maximum()
    {
        // arrange
        RegisterBillingPlanCommand command = CreateValidCommand() with
        {
            FreePlanDailyRate = MaximumRate + 0.01m
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterBillingPlanCommand.FreePlanDailyRate) &&
            e.ErrorMessage == _freePlanDailyRateMaxMessage));
    }
}