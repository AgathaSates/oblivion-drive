using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Vehicle;
using OblivionDrive.Application.VehicleModule.Commands;

namespace OblivionDrive.Tests.Unit.VehicleModule.ValidatorTests;

[TestClass]
[TestCategory("Vehicle - DeleteVehicleCommandValidator Unit Tests")]
public class DeleteVehicleCommandValidatorTests
{
    private DeleteVehicleCommandValidator _validator = null!;

    private const string VehicleIdRequiredMessage =
        "O identificador do veículo é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteVehicleCommandValidator();
    }

    private static DeleteVehicleCommand CreateValidCommand()
    {
        return new DeleteVehicleCommand(
            VehicleId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_VehicleId_Is_Valid()
    {
        // arrange
        DeleteVehicleCommand command = CreateValidCommand();

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
        DeleteVehicleCommand command = CreateValidCommand() with { VehicleId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(DeleteVehicleCommand.VehicleId) &&
            error.ErrorMessage == VehicleIdRequiredMessage));
    }
}