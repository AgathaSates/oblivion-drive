using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Rental;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Tests.Unit.RentalModule.ValidatorTests;

[TestClass]
[TestCategory("Rental - RegisterRentalCommandValidator Unit Tests")]
public class RegisterRentalCommandValidatorTests
{
    private RegisterRentalCommandValidator _validator = null!;

    private readonly DateOnly _minimumDate = new(2000, 1, 1);

    private const string ClientIdRequiredMessage =
        "O identificador do cliente é obrigatório.";

    private const string DriverIdRequiredMessage =
        "O identificador do condutor é obrigatório.";

    private const string VehicleIdRequiredMessage =
        "O identificador do veículo é obrigatório.";

    private const string PlanTypeInvalidMessage =
        "O tipo de plano selecionado é inválido.";

    private string StartDateMinMessage =>
        $"A data de saída não pode ser anterior a {_minimumDate:dd/MM/yyyy}.";

    private const string ExpectedReturnDateMinMessage =
        "A data prevista de retorno deve ser maior ou igual à data de saída.";

    private const string InsuranceDailyPriceNonNegativeMessage =
        "O valor diário do seguro por pessoa não pode ser negativo.";

    private const string InsurancePersonsCountNonNegativeMessage =
        "A quantidade de pessoas para o seguro não pode ser negativa.";

    private const string EstimatedTotalKilometersRequiredForControlledMessage =
        "A quilometragem estimada é obrigatória para o Plano Controlado.";

    private const string EstimatedTotalKilometersGreaterThanZeroForControlledMessage =
        "A quilometragem estimada deve ser maior que zero para o Plano Controlado.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new RegisterRentalCommandValidator();
    }

    private static RegisterRentalCommand CreateValidCommand(RentalPlanType planType = RentalPlanType.Free)
    {
        return new RegisterRentalCommand(
            ClientId: Guid.NewGuid(),
            DriverId: Guid.NewGuid(),
            VehicleId: Guid.NewGuid(),
            PlanType: planType,
            StartDate: new DateOnly(2025, 1, 10),
            ExpectedReturnDate: new DateOnly(2025, 1, 12),
            InsuranceDailyPricePerPerson: 10m,
            InsurancePersonsCount: 2,
            EstimatedTotalKilometers: null,
            ServiceIds: null
        );
    }


    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid_For_Free_Plan()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(RentalPlanType.Free);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid_For_Controlled_Plan_With_EstimatedKm()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(RentalPlanType.Controlled) with
        {
            EstimatedTotalKilometers = 100
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_ClientId_Is_Empty()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand() with { ClientId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.ClientId) &&
            error.ErrorMessage == ClientIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_DriverId_Is_Empty()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand() with { DriverId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.DriverId) &&
            error.ErrorMessage == DriverIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_VehicleId_Is_Empty()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand() with { VehicleId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.VehicleId) &&
            error.ErrorMessage == VehicleIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_PlanType_Is_Invalid()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand() with { PlanType = (RentalPlanType)999 };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.PlanType) &&
            error.ErrorMessage == PlanTypeInvalidMessage));
    }

    [TestMethod]
    public void Should_Fail_When_StartDate_Is_Before_MinimumDate()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand() with
        {
            StartDate = new DateOnly(1999, 12, 31),
            ExpectedReturnDate = new DateOnly(2000, 1, 1),
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.StartDate) &&
            error.ErrorMessage == StartDateMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ExpectedReturnDate_Is_Before_StartDate()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand() with
        {
            StartDate = new DateOnly(2025, 1, 10),
            ExpectedReturnDate = new DateOnly(2025, 1, 9),
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.ExpectedReturnDate) &&
            error.ErrorMessage == ExpectedReturnDateMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_InsuranceDailyPricePerPerson_Is_Negative()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand() with { InsuranceDailyPricePerPerson = -0.01m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.InsuranceDailyPricePerPerson) &&
            error.ErrorMessage == InsuranceDailyPriceNonNegativeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_InsurancePersonsCount_Is_Negative()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand() with { InsurancePersonsCount = -1 };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.InsurancePersonsCount) &&
            error.ErrorMessage == InsurancePersonsCountNonNegativeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_EstimatedTotalKilometers_Is_Null_For_Controlled_Plan()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(RentalPlanType.Controlled) with
        {
            EstimatedTotalKilometers = null
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.EstimatedTotalKilometers) &&
            error.ErrorMessage == EstimatedTotalKilometersRequiredForControlledMessage));
    }

    [TestMethod]
    public void Should_Fail_When_EstimatedTotalKilometers_Is_Zero_For_Controlled_Plan()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(RentalPlanType.Controlled) with
        {
            EstimatedTotalKilometers = 0
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.EstimatedTotalKilometers) &&
            error.ErrorMessage == EstimatedTotalKilometersGreaterThanZeroForControlledMessage));
    }

    [TestMethod]
    public void Should_Fail_When_EstimatedTotalKilometers_Is_Negative_For_Controlled_Plan()
    {
        // arrange
        RegisterRentalCommand command = CreateValidCommand(RentalPlanType.Controlled) with
        {
            EstimatedTotalKilometers = -10
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterRentalCommand.EstimatedTotalKilometers) &&
            error.ErrorMessage == EstimatedTotalKilometersGreaterThanZeroForControlledMessage));
    }
}