using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Services;
using OblivionDrive.Application.ServicesModule.Commands;

namespace OblivionDrive.Tests.Unit.ServicesModule.ValidatorTests;

[TestClass]
[TestCategory("Service - DeleteServiceCommandValidator Unit Tests")]
public class DeleteServiceCommandValidatorTests
{
    private DeleteServiceCommandValidator _validator = null!;

    private const string ServiceIdRequiredMessage =
        "O identificador do serviço é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteServiceCommandValidator();
    }

    private static DeleteServiceCommand CreateValidCommand()
    {
        return new DeleteServiceCommand(
            ServiceId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_ServiceId_Is_Valid()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_ServiceId_Is_Empty()
    {
        // arrange
        DeleteServiceCommand command = CreateValidCommand() with { ServiceId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(DeleteServiceCommand.ServiceId) &&
            error.ErrorMessage == ServiceIdRequiredMessage));
    }
}
