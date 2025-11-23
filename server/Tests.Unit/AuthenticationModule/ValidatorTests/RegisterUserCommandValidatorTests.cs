using FluentValidation.Results;
using OblivionDrive.Application.AuthenticationModule.Commands;
using OblivionDrive.Application.FluentValidation.Authentication;

namespace OblivionDrive.Tests.Unit.AuthenticationModule.ValidatorTests;

[TestClass]
[TestCategory("Authentication - RegisterUserCommandValidator Unit Tests")]
public class RegisterUserCommandValidatorTests
{
    private RegisterUserCommandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new RegisterUserCommandValidator();
    }

    [TestMethod]
    public void Should_Fail_When_UserName_Is_Empty()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: string.Empty,
            Email: "user@example.com",
            Password: "Senha123!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.UserName) &&
            e.ErrorMessage == "O nome de usuário é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_UserName_Is_Shorter_Than_Three_Characters()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "ab",
            Email: "user@example.com",
            Password: "Senha123!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.UserName) &&
            e.ErrorMessage == "O nome de usuário deve ter pelo menos 3 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Empty()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "user123",
            Email: string.Empty,
            Password: "Senha123!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Email) &&
            e.ErrorMessage == "O e-mail é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Invalid()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "user123",
            Email: "invalid-email",
            Password: "Senha123!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Email) &&
            e.ErrorMessage == "O e-mail deve estar no formato [ nome@dominio.com ]."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Is_Empty()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "user123",
            Email: "user@example.com",
            Password: string.Empty
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Password) &&
            e.ErrorMessage == "A senha é obrigatória."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Is_Shorter_Than_Minimum_Length()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "user123",
            Email: "user@example.com",
            Password: "Ab1!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Password) &&
            e.ErrorMessage == "A senha deve ter pelo menos 6 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Does_Not_Match_Strength_Rules()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "user123",
            Email: "user@example.com",
            Password: "Senha123"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Password) &&
            e.ErrorMessage == "A senha deve conter pelo menos uma letra maiúscula, uma letra minúscula, um número e um caractere especial."));
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        var command = new RegisterUserCommand(
            UserName: "validUser",
            Email: "user@example.com",
            Password: "Senha123!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
}