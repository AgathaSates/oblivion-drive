using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Rental;
using OblivionDrive.Application.RentalModule.Commands;

namespace OblivionDrive.Tests.Unit.RentalModule.ValidatorTests;

[TestClass]
[TestCategory("Rental - DeleteRentalCommandValidator Unit Tests")]
public class DeleteRentalCommandValidatorTests
{
    private DeleteRentalCommandValidator _validator = null!;

    private const string RentalIdRequiredMessage =
        "O identificador do aluguel é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteRentalCommandValidator();
    }

    private static DeleteRentalCommand CreateValidCommand()
    {
        return new DeleteRentalCommand(
            RentalId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_RentalId_Is_Valid()
    {
        // arrange
        DeleteRentalCommand command = CreateValidCommand();

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
        DeleteRentalCommand command = CreateValidCommand() with { RentalId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(DeleteRentalCommand.RentalId) &&
            error.ErrorMessage == RentalIdRequiredMessage));
    }
}