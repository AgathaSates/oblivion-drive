using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Rental;
using OblivionDrive.Application.RentalModule.Commands;

namespace OblivionDrive.Tests.Unit.RentalModule.ValidatorTests;

[TestClass]
[TestCategory("Rental - SendRentalReceiptEmailCommandValidator Unit Tests")]
public class SendRentalReceiptEmailCommandValidatorTests
{
    private SendRentalReceiptEmailCommandValidator _validator = null!;

    private const string RentalIdRequiredMessage =
        "O identificador do aluguel é obrigatório.";

    private const string EmailRequiredMessage =
        "O e-mail é obrigatório.";

    private const string EmailInvalidMessage =
        "O e-mail informado é inválido.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new SendRentalReceiptEmailCommandValidator();
    }

    private static SendRentalReceiptEmailCommand CreateValidCommand()
        => new SendRentalReceiptEmailCommand(
            RentalId: Guid.NewGuid(),
            Email: "cliente@exemplo.com"
        );

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand();

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
        SendRentalReceiptEmailCommand command = CreateValidCommand() with { RentalId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(SendRentalReceiptEmailCommand.RentalId) &&
            error.ErrorMessage == RentalIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Empty()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand() with { Email = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(SendRentalReceiptEmailCommand.Email) &&
            error.ErrorMessage == EmailRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Invalid()
    {
        // arrange
        SendRentalReceiptEmailCommand command = CreateValidCommand() with { Email = "email-invalido" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(SendRentalReceiptEmailCommand.Email) &&
            error.ErrorMessage == EmailInvalidMessage));
    }
}