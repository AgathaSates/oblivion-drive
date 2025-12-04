
using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.VehicleGroup;
using OblivionDrive.Application.VehicleGroupModule.commands;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule.ValidatorTests;

[TestClass]
[TestCategory("VehicleGroup - RegisterVehicleGroupCommandValidator Unit Tests")]
public class RegisterVehicleGroupCommandValidatorTests
{
    private RegisterVehicleGroupCommandValidator _validator = null!;

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    private readonly string _nameRequiredMessage =
        "O nome do grupo de veículos é obrigatório.";

    private readonly string _nameMinMessage =
        $"O nome do grupo de veículos deve ter pelo menos {MinimumNameLength} caracteres.";

    private readonly string _nameMaxMessage =
        $"O nome do grupo de veículos deve ter no máximo {MaximumNameLength} caracteres.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new RegisterVehicleGroupCommandValidator();
    }

    private static RegisterVehicleGroupCommand CreateValidCommand()
    {
        return new RegisterVehicleGroupCommand(
            Name: "Grupo válido"
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        RegisterVehicleGroupCommand command = CreateValidCommand();

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
        RegisterVehicleGroupCommand command = CreateValidCommand() with { Name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterVehicleGroupCommand.Name) &&
            e.ErrorMessage == _nameRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        RegisterVehicleGroupCommand command = CreateValidCommand() with { Name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterVehicleGroupCommand.Name) &&
            e.ErrorMessage == _nameMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        string longName = new string('A', MaximumNameLength + 1);
        RegisterVehicleGroupCommand command = CreateValidCommand() with { Name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterVehicleGroupCommand.Name) &&
            e.ErrorMessage == _nameMaxMessage));
    }
}
