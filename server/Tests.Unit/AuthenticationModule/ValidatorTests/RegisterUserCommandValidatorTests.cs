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

    private static RegisterUserCommand CreateValidCommand()
    {
        return new RegisterUserCommand(
            UserName: "validUser",
            Email: "user@example.com",
            Password: "Senha123!"
        );
    }

    [TestMethod]
    public void Should_Fail_When_UserName_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { UserName = string.Empty };

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
        var command = CreateValidCommand() with { UserName = "ab" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.UserName) &&
            e.ErrorMessage == "O nome de usuário deve ter pelo menos 3 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_UserName_Is_Greater_Than_Maximum_Length()
    {
        // arrange
        var longUserName = new string('a', 101);
        var command = CreateValidCommand() with { UserName = longUserName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.UserName) &&
            e.ErrorMessage == "O nome de usuário deve ter no máximo 100 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_UserName_Contains_Whitespace()
    {
        // arrange
        var command = CreateValidCommand() with { UserName = "invalid user" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.UserName) &&
            e.ErrorMessage == "O nome de usuário não deve conter espaços em branco."));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { Email = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Email) &&
            e.ErrorMessage == "O e-mail é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Greater_Than_Maximum_Length()
    {
        // arrange
        var longLocalPart = new string('a', 300);
        var command = CreateValidCommand() with { Email = $"{longLocalPart}@test.com" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Email) &&
            e.ErrorMessage == "O e-mail deve ter no máximo 256 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Invalid()
    {
        // arrange
        var command = CreateValidCommand() with { Email = "invalid-email" };

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
        var command = CreateValidCommand() with { Password = string.Empty };

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
        var command = CreateValidCommand() with { Password = "Ab1!" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Password) &&
            e.ErrorMessage == "A senha deve ter pelo menos 6 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Is_Greater_Than_Maximum_Length()
    {
        // arrange
        var longPassword = "Ab1!" + new string('x', 97);
        var command = CreateValidCommand() with { Password = longPassword };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterUserCommand.Password) &&
            e.ErrorMessage == "A senha deve ter no máximo 100 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Does_Not_Match_Strength_Rules()
    {
        // arrange
        var command = CreateValidCommand() with { Password = "Senha123" };

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
        var command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
}