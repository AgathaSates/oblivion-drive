using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.VehicleGroup;
using OblivionDrive.Application.VehicleGroupModule.commands;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule.ValidatorTests;

[TestClass]
[TestCategory("VehicleGroup - DeleteVehicleGroupCommandValidator Unit Tests")]
public class DeleteVehicleGroupCommandValidatorTests
{
    private DeleteVehicleGroupCommandValidator _validator = null!;

    private readonly string _vehicleGroupIdRequiredMessage =
        "O identificador do grupo de veículos é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteVehicleGroupCommandValidator();
    }

    private static DeleteVehicleGroupCommand CreateValidCommand()
    {
        return new DeleteVehicleGroupCommand(
            VehicleGroupId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        DeleteVehicleGroupCommand command = CreateValidCommand();

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
        DeleteVehicleGroupCommand command = CreateValidCommand() with { VehicleGroupId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(DeleteVehicleGroupCommand.VehicleGroupId) &&
            e.ErrorMessage == _vehicleGroupIdRequiredMessage));
    }
}