using FluentValidation.Results;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Application.FluentValidation.Client;

namespace OblivionDrive.Tests.Unit.ClientModule.ValidatorTests;

[TestClass]
[TestCategory("Client - DeleteClientCommandValidator Unit Tests")]
public class DeleteClientCommandValidatorTests
{
    private DeleteClientCommandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteClientCommandValidator();
    }

    private static DeleteClientCommand CreateValidCommand()
    {
        return new DeleteClientCommand(
            ClientId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_ClientId_Is_Empty()
    {
        // arrange
        DeleteClientCommand command = CreateValidCommand() with { ClientId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(DeleteClientCommand.ClientId) &&
            e.ErrorMessage == "O identificador do cliente é obrigatório."));
    }
}
