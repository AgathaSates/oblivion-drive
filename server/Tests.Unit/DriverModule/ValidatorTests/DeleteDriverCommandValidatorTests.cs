
using FluentValidation.Results;
using OblivionDrive.Application.DriverModule.Commands;
using OblivionDrive.Application.FluentValidation.Driver;

namespace OblivionDrive.Tests.Unit.DriverModule.ValidatorTests;

[TestClass]
[TestCategory("Driver - DeleteDriverCommandValidator Unit Tests")]
public class DeleteDriverCommandValidatorTests
{
    private DeleteDriverCommandValidator _validator = null!;

    private const string DriverIdRequiredMessage =
        "O identificador do condutor é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteDriverCommandValidator();
    }

    private static DeleteDriverCommand CreateValidCommand()
    {
        return new DeleteDriverCommand(
            DriverId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_DriverId_Is_Valid()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_DriverId_Is_Empty()
    {
        // arrange
        DeleteDriverCommand command = CreateValidCommand() with { DriverId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(DeleteDriverCommand.DriverId) &&
            error.ErrorMessage == DriverIdRequiredMessage));
    }
}