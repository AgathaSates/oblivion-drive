using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Vehicle;
using OblivionDrive.Application.VehicleModule.Commands;

namespace OblivionDrive.Tests.Unit.VehicleModule.ValidatorTests;

[TestClass]
[TestCategory("Vehicle - UpdateVehicleCommandValidator Unit Tests")]
public class UpdateVehicleCommandValidatorTests
{
    private UpdateVehicleCommandValidator _validator = null!;

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

    private readonly string _vehicleIdRequiredMessage =
        "O identificador do veículo é obrigatório.";

    private readonly string _brandRequiredMessage =
        "A marca do veículo é obrigatória.";

    private readonly string _brandMinMessage =
        $"A marca do veículo deve ter pelo menos {MinimumBrandLength} caracteres.";

    private readonly string _brandMaxMessage =
        $"A marca do veículo deve ter no máximo {MaximumBrandLength} caracteres.";

    private readonly string _modelRequiredMessage =
        "O modelo do veículo é obrigatório.";

    private readonly string _modelMinMessage =
        $"O modelo do veículo deve ter pelo menos {MinimumModelLength} caractere(s).";

    private readonly string _modelMaxMessage =
        $"O modelo do veículo deve ter no máximo {MaximumModelLength} caracteres.";

    private readonly string _colorRequiredMessage =
        "A cor do veículo é obrigatória.";

    private readonly string _colorMinMessage =
        $"A cor do veículo deve ter pelo menos {MinimumColorLength} caractere(s).";

    private readonly string _colorMaxMessage =
        $"A cor do veículo deve ter no máximo {MaximumColorLength} caracteres.";

    private readonly string _fuelTankCapacityMinMessage =
        $"A capacidade do tanque deve ser maior ou igual a {MinimumFuelTankCapacity}.";

    private readonly string _fuelTankCapacityMaxMessage =
        $"A capacidade do tanque não pode ser maior que {MaximumFuelTankCapacity} litros.";

    private readonly string _yearRangeMessage =
        $"O ano do veículo deve estar entre {MinimumYear} e {DateTime.UtcNow.Year + 1}.";

    private readonly string _vehicleGroupIdRequiredMessage =
        "O identificador do grupo de veículos é obrigatório.";

    private readonly string _photoEmptyMessage =
        "A foto do veículo, se informada, não pode estar vazia.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateVehicleCommandValidator();
    }

    private static UpdateVehicleCommand CreateValidCommand()
    {
        int currentYear = DateTime.UtcNow.Year;

        return new UpdateVehicleCommand(
            VehicleId: Guid.NewGuid(),
            Brand: "Toyota",
            Model: "Corolla",
            Color: "White",
            FuelType: 0,
            FuelTankCapacityInLiters: 55.5m,
            Year: currentYear,
            VehicleGroupId: Guid.NewGuid(),
            PhotoBytes: new byte[] { 1, 2, 3 }
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_VehicleId_Is_Empty()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand() with { VehicleId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.VehicleId) &&
            error.ErrorMessage == _vehicleIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Brand_Is_Empty()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand() with { Brand = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Brand) &&
            error.ErrorMessage == _brandRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Brand_Is_Too_Short()
    {
        // arrange
        string shortBrand = new string('A', MinimumBrandLength - 1);

        UpdateVehicleCommand command = CreateValidCommand() with { Brand = shortBrand };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Brand) &&
            error.ErrorMessage == _brandMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Brand_Is_Too_Long()
    {
        // arrange
        string longBrand = new string('A', MaximumBrandLength + 1);

        UpdateVehicleCommand command = CreateValidCommand() with { Brand = longBrand };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Brand) &&
            error.ErrorMessage == _brandMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Model_Is_Empty()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand() with { Model = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Model) &&
            error.ErrorMessage == _modelRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Model_Is_Too_Short()
    {
        // arrange
        string shortModel = new string('A', MinimumModelLength - 1);

        UpdateVehicleCommand command = CreateValidCommand() with { Model = shortModel };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Model) &&
            error.ErrorMessage == _modelMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Model_Is_Too_Long()
    {
        // arrange
        string longModel = new string('A', MaximumModelLength + 1);

        UpdateVehicleCommand command = CreateValidCommand() with { Model = longModel };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Model) &&
            error.ErrorMessage == _modelMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Color_Is_Empty()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand() with { Color = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Color) &&
            error.ErrorMessage == _colorRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Color_Is_Too_Short()
    {
        // arrange
        string shortColor = new string('A', MinimumColorLength - 1);

        UpdateVehicleCommand command = CreateValidCommand() with { Color = shortColor };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Color) &&
            error.ErrorMessage == _colorMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Color_Is_Too_Long()
    {
        // arrange
        string longColor = new string('A', MaximumColorLength + 1);

        UpdateVehicleCommand command = CreateValidCommand() with { Color = longColor };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Color) &&
            error.ErrorMessage == _colorMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_FuelTankCapacity_Is_Less_Than_Minimum()
    {
        // arrange
        decimal invalidFuelTankCapacity = MinimumFuelTankCapacity - 0.01m;

        UpdateVehicleCommand command = CreateValidCommand() with
        {
            FuelTankCapacityInLiters = invalidFuelTankCapacity
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.FuelTankCapacityInLiters) &&
            error.ErrorMessage == _fuelTankCapacityMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_FuelTankCapacity_Is_Greater_Than_Maximum()
    {
        // arrange
        decimal invalidFuelTankCapacity = MaximumFuelTankCapacity + 0.01m;

        UpdateVehicleCommand command = CreateValidCommand() with
        {
            FuelTankCapacityInLiters = invalidFuelTankCapacity
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.FuelTankCapacityInLiters) &&
            error.ErrorMessage == _fuelTankCapacityMaxMessage));
    }

    [TestMethod]
    public void Should_Pass_When_Year_Is_Within_Range()
    {
        // arrange
        int validYear = DateTime.UtcNow.Year;

        UpdateVehicleCommand command = CreateValidCommand() with { Year = validYear };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_Year_Is_Less_Than_Minimum()
    {
        // arrange
        int invalidYear = MinimumYear - 1;

        UpdateVehicleCommand command = CreateValidCommand() with { Year = invalidYear };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Year) &&
            error.ErrorMessage == _yearRangeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Year_Is_Greater_Than_Maximum()
    {
        // arrange
        int maximumYear = MaximumYear;
        int invalidYear = maximumYear + 1;

        UpdateVehicleCommand command = CreateValidCommand() with { Year = invalidYear };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.Year) &&
            error.ErrorMessage == _yearRangeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_VehicleGroupId_Is_Empty()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand() with { VehicleGroupId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.VehicleGroupId) &&
            error.ErrorMessage == _vehicleGroupIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Pass_When_PhotoBytes_Is_Null()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand() with { PhotoBytes = null };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_PhotoBytes_Is_Empty()
    {
        // arrange
        UpdateVehicleCommand command = CreateValidCommand() with { PhotoBytes = Array.Empty<byte>() };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdateVehicleCommand.PhotoBytes) &&
            error.ErrorMessage == _photoEmptyMessage));
    }
}