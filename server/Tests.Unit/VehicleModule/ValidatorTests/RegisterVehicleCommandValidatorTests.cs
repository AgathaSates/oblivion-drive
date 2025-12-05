using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Vehicle;
using OblivionDrive.Application.VehicleModule.Commands;
using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Tests.Unit.VehicleModule.ValidatorTests;

[TestClass]
[TestCategory("Vehicle - RegisterVehicleCommandValidator Unit Tests")]
public class RegisterVehicleCommandValidatorTests
{
    private RegisterVehicleCommandValidator _validator = null!;

    private const int MinimumLicensePlateLength = 2;
    private const int MaximumLicensePlateLength = 20;

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

    private readonly string _licensePlateRequiredMessage =
        "A placa do veículo é obrigatória.";

    private readonly string _licensePlateMinMessage =
        $"A placa do veículo deve ter pelo menos {MinimumLicensePlateLength} caracteres.";

    private readonly string _licensePlateMaxMessage =
        $"A placa do veículo deve ter no máximo {MaximumLicensePlateLength} caracteres.";

    private readonly string _brandRequiredMessage =
        "A marca do veículo é obrigatória.";

    private readonly string _brandMinMessage =
        $"A marca do veículo deve ter pelo menos {MinimumBrandLength} caracteres.";

    private readonly string _brandMaxMessage =
        $"A marca do veículo deve ter no máximo {MaximumBrandLength} caracteres.";

    private readonly string _modelRequiredMessage =
        "O modelo do veículo é obrigatório.";

    private readonly string _modelMaxMessage =
        $"O modelo do veículo deve ter no máximo {MaximumModelLength} caracteres.";

    private readonly string _colorRequiredMessage =
        "A cor do veículo é obrigatória.";

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

    private readonly string _photoRequiredMessage =
        "A foto do veículo é obrigatória.";

    private readonly string _photoEmptyMessage =
        "A foto do veículo não pode estar vazia.";

    private readonly string _fuelTypeInvalidMessage =
    "O tipo de combustível informado é inválido.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new RegisterVehicleCommandValidator();
    }

    private static RegisterVehicleCommand CreateValidCommand()
    {
        int currentYear = DateTime.UtcNow.Year;

        return new RegisterVehicleCommand(
            LicensePlate: "ABC1D23",
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
        RegisterVehicleCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_LicensePlate_Is_Empty()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand() with { LicensePlate = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.LicensePlate) &&
            error.ErrorMessage == _licensePlateRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_LicensePlate_Is_Too_Short()
    {
        // arrange
        string shortLicensePlate = new string('A', MinimumLicensePlateLength - 1);

        RegisterVehicleCommand command = CreateValidCommand() with { LicensePlate = shortLicensePlate };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.LicensePlate) &&
            error.ErrorMessage == _licensePlateMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_LicensePlate_Is_Too_Long()
    {
        // arrange
        string longLicensePlate = new string('A', MaximumLicensePlateLength + 1);

        RegisterVehicleCommand command = CreateValidCommand() with { LicensePlate = longLicensePlate };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.LicensePlate) &&
            error.ErrorMessage == _licensePlateMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Brand_Is_Empty()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand() with { Brand = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Brand) &&
            error.ErrorMessage == _brandRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Brand_Is_Too_Short()
    {
        // arrange
        string shortBrand = new string('A', MinimumBrandLength - 1);

        RegisterVehicleCommand command = CreateValidCommand() with { Brand = shortBrand };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Brand) &&
            error.ErrorMessage == _brandMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Brand_Is_Too_Long()
    {
        // arrange
        string longBrand = new string('A', MaximumBrandLength + 1);

        RegisterVehicleCommand command = CreateValidCommand() with { Brand = longBrand };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Brand) &&
            error.ErrorMessage == _brandMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_FuelType_Is_Invalid()
    {
        // arrange
        var command = CreateValidCommand() with { FuelType = (FuelType)999 };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterVehicleCommand.FuelType) &&
            e.ErrorMessage == _fuelTypeInvalidMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Model_Is_Empty()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand() with { Model = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Model) &&
            error.ErrorMessage == _modelRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Model_Is_Too_Long()
    {
        // arrange
        string longModel = new string('A', MaximumModelLength + 1);

        RegisterVehicleCommand command = CreateValidCommand() with { Model = longModel };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Model) &&
            error.ErrorMessage == _modelMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Color_Is_Empty()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand() with { Color = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Color) &&
            error.ErrorMessage == _colorRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Color_Is_Too_Long()
    {
        // arrange
        string longColor = new string('A', MaximumColorLength + 1);

        RegisterVehicleCommand command = CreateValidCommand() with { Color = longColor };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Color) &&
            error.ErrorMessage == _colorMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_FuelTankCapacity_Is_Less_Than_Minimum()
    {
        // arrange
        decimal invalidFuelTankCapacity = MinimumFuelTankCapacity - 0.01m;

        RegisterVehicleCommand command = CreateValidCommand() with
        {
            FuelTankCapacityInLiters = invalidFuelTankCapacity
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.FuelTankCapacityInLiters) &&
            error.ErrorMessage == _fuelTankCapacityMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_FuelTankCapacity_Is_Greater_Than_Maximum()
    {
        // arrange
        decimal invalidFuelTankCapacity = MaximumFuelTankCapacity + 0.01m;

        RegisterVehicleCommand command = CreateValidCommand() with
        {
            FuelTankCapacityInLiters = invalidFuelTankCapacity
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.FuelTankCapacityInLiters) &&
            error.ErrorMessage == _fuelTankCapacityMaxMessage));
    }

    [TestMethod]
    public void Should_Pass_When_Year_Is_Within_Range()
    {
        // arrange
        int validYear = DateTime.UtcNow.Year;

        RegisterVehicleCommand command = CreateValidCommand() with { Year = validYear };

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

        RegisterVehicleCommand command = CreateValidCommand() with { Year = invalidYear };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Year) &&
            error.ErrorMessage == _yearRangeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Year_Is_Greater_Than_Maximum()
    {
        // arrange
        int maximumYear = MaximumYear;
        int invalidYear = maximumYear + 1;

        RegisterVehicleCommand command = CreateValidCommand() with { Year = invalidYear };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.Year) &&
            error.ErrorMessage == _yearRangeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_VehicleGroupId_Is_Empty()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand() with { VehicleGroupId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.VehicleGroupId) &&
            error.ErrorMessage == _vehicleGroupIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_PhotoBytes_Is_Null()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand() with { PhotoBytes = null! };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.PhotoBytes) &&
            error.ErrorMessage == _photoRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_PhotoBytes_Is_Empty()
    {
        // arrange
        RegisterVehicleCommand command = CreateValidCommand() with { PhotoBytes = Array.Empty<byte>() };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterVehicleCommand.PhotoBytes) &&
            error.ErrorMessage == _photoEmptyMessage));
    }
}
