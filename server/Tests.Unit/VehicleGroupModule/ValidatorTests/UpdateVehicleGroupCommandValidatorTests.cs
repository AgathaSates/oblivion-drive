using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.VehicleGroup;
using OblivionDrive.Application.VehicleGroupModule.commands;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule.ValidatorTests;

[TestClass]
[TestCategory("VehicleGroup - UpdateVehicleGroupCommandValidator Unit Tests")]
public class UpdateVehicleGroupCommandValidatorTests
{
    private UpdateVehicleGroupCommandValidator _validator = null!;

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    private readonly string _vehicleGroupIdRequiredMessage =
        "O identificador do grupo de veículos é obrigatório.";

    private readonly string _nameRequiredMessage =
        "O nome do grupo de veículos é obrigatório.";

    private readonly string _nameMinMessage =
        $"O nome do grupo de veículos deve ter pelo menos {MinimumNameLength} caracteres.";

    private readonly string _nameMaxMessage =
        $"O nome do grupo de veículos deve ter no máximo {MaximumNameLength} caracteres.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateVehicleGroupCommandValidator();
    }

    private static UpdateVehicleGroupCommand CreateValidCommand()
    {
        return new UpdateVehicleGroupCommand(
            VehicleGroupId: Guid.NewGuid(),
            name: "Grupo atualizado válido"
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_VehicleGroupId_Is_Empty()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand() with { VehicleGroupId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateVehicleGroupCommand.VehicleGroupId) &&
            e.ErrorMessage == _vehicleGroupIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Empty()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand() with { name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateVehicleGroupCommand.name) &&
            e.ErrorMessage == _nameRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        UpdateVehicleGroupCommand command = CreateValidCommand() with { name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateVehicleGroupCommand.name) &&
            e.ErrorMessage == _nameMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        string longName = new string('A', MaximumNameLength + 1);
        UpdateVehicleGroupCommand command = CreateValidCommand() with { name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateVehicleGroupCommand.name) &&
            e.ErrorMessage == _nameMaxMessage));
    }
}