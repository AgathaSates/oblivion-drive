using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Rental;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Tests.Unit.RentalModule.ValidatorTests;

[TestClass]
[TestCategory("Rental - UpdateRentalCommandValidator Unit Tests")]
public class UpdateRentalCommandValidatorTests
{
    private UpdateRentalCommandValidator _validator = null!;

    private readonly DateOnly _minimumDate = new(2000, 1, 1);

    private const string RentalIdRequiredMessage = "O identificador do aluguel é obrigatório.";
    private const string ClientIdRequiredMessage = "O identificador do cliente é obrigatório.";
    private const string DriverIdRequiredMessage = "O identificador do condutor é obrigatório.";
    private const string VehicleIdRequiredMessage = "O identificador do veículo é obrigatório.";

    private const string PlanTypeInvalidMessage = "O tipo de plano selecionado é inválido.";

    private const string ExpectedReturnDateInvalidMessage =
        "A data prevista de retorno deve ser maior ou igual à data de saída.";

    private const string InsuranceDailyPriceNegativeMessage =
        "O valor diário do seguro por pessoa não pode ser negativo.";

    private const string InsurancePersonsCountNegativeMessage =
        "A quantidade de pessoas para o seguro não pode ser negativa.";

    private const string EstimatedKmRequiredForControlledMessage =
        "A quilometragem estimada é obrigatória para o Plano Controlado.";

    private const string EstimatedKmGreaterThanZeroForControlledMessage =
        "A quilometragem estimada deve ser maior que zero para o Plano Controlado.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateRentalCommandValidator();
    }

    private UpdateRentalCommand CreateValidCommand(RentalPlanType planType = RentalPlanType.Free)
    {
        DateOnly startDate = new(2025, 1, 10);
        DateOnly expectedReturnDate = new(2025, 1, 12);

        int? estimatedKm = planType == RentalPlanType.Controlled ? 100 : null;

        return new UpdateRentalCommand(
            RentalId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            DriverId: Guid.NewGuid(),
            VehicleId: Guid.NewGuid(),
            PlanType: planType,
            StartDate: startDate,
            ExpectedReturnDate: expectedReturnDate,
            InsuranceDailyPricePerPerson: 10m,
            InsurancePersonsCount: 1,
            EstimatedTotalKilometers: estimatedKm,
            ServiceIds: null
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_RentalId_Is_Empty()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand() with { RentalId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.RentalId) &&
            e.ErrorMessage == RentalIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ClientId_Is_Empty()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand() with { ClientId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.ClientId) &&
            e.ErrorMessage == ClientIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_DriverId_Is_Empty()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand() with { DriverId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.DriverId) &&
            e.ErrorMessage == DriverIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_VehicleId_Is_Empty()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand() with { VehicleId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.VehicleId) &&
            e.ErrorMessage == VehicleIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_PlanType_Is_Invalid()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand() with { PlanType = (RentalPlanType)999 };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.PlanType) &&
            e.ErrorMessage == PlanTypeInvalidMessage));
    }

    [TestMethod]
    public void Should_Fail_When_StartDate_Is_Before_MinimumDate()
    {
        // arrange
        DateOnly invalidStartDate = new(1999, 12, 31);

        UpdateRentalCommand command = CreateValidCommand() with { StartDate = invalidStartDate };

        string expectedMessage =
            $"A data de saída não pode ser anterior a {_minimumDate:dd/MM/yyyy}.";

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.StartDate) &&
            e.ErrorMessage == expectedMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ExpectedReturnDate_Is_Before_StartDate()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand() with
        {
            StartDate = new DateOnly(2025, 1, 10),
            ExpectedReturnDate = new DateOnly(2025, 1, 9)
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.ExpectedReturnDate) &&
            e.ErrorMessage == ExpectedReturnDateInvalidMessage));
    }

    [TestMethod]
    public void Should_Fail_When_InsuranceDailyPricePerPerson_Is_Negative()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand() with { InsuranceDailyPricePerPerson = -0.01m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.InsuranceDailyPricePerPerson) &&
            e.ErrorMessage == InsuranceDailyPriceNegativeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_InsurancePersonsCount_Is_Negative()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand() with { InsurancePersonsCount = -1 };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.InsurancePersonsCount) &&
            e.ErrorMessage == InsurancePersonsCountNegativeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_EstimatedTotalKilometers_Is_Null_For_Controlled_Plan()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand(RentalPlanType.Controlled) with
        {
            EstimatedTotalKilometers = null
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.EstimatedTotalKilometers) &&
            e.ErrorMessage == EstimatedKmRequiredForControlledMessage));
    }

    [TestMethod]
    public void Should_Fail_When_EstimatedTotalKilometers_Is_Zero_For_Controlled_Plan()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand(RentalPlanType.Controlled) with
        {
            EstimatedTotalKilometers = 0
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateRentalCommand.EstimatedTotalKilometers) &&
            e.ErrorMessage == EstimatedKmGreaterThanZeroForControlledMessage));
    }

    [TestMethod]
    public void Should_Pass_When_EstimatedTotalKilometers_Is_Null_For_Free_Plan()
    {
        // arrange
        UpdateRentalCommand command = CreateValidCommand(RentalPlanType.Free) with
        {
            EstimatedTotalKilometers = null
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
}