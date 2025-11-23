using FluentValidation.Results;
using OblivionDrive.Application.AuthenticationModule.Commands;
using OblivionDrive.Application.FluentValidation.Authentication;

namespace OblivionDrive.Tests.Unit.AuthenticationModule.ValidatorTests;

[TestClass]
[TestCategory("Authentication - LoginUserCommandValidator Unit Tests")]
public class LoginUserCommandValidatorTests
{
    private LoginUserCommandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new LoginUserCommandValidator();
    }

    [TestMethod]
    public void Should_Fail_When_UserName_Is_Empty()
    {
        // arrange
        var command = new LoginUserCommand(
            UserName: string.Empty,
            Password: "Senha123!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(LoginUserCommand.UserName) &&
            e.ErrorMessage == "O nome de usuário é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_UserName_Is_Shorter_Than_Three_Characters()
    {
        // arrange
        var command = new LoginUserCommand(
            UserName: "ab",
            Password: "Senha123!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(LoginUserCommand.UserName) &&
            e.ErrorMessage == "O nome de usuário deve ter pelo menos 3 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Is_Empty()
    {
        // arrange
        var command = new LoginUserCommand(
            UserName: "validUser",
            Password: string.Empty
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(LoginUserCommand.Password) &&
            e.ErrorMessage == "A senha é obrigatória."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Is_Shorter_Than_Minimum_Length()
    {
        // arrange
        var command = new LoginUserCommand(
            UserName: "validUser",
            Password: "Ab1!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(LoginUserCommand.Password) &&
            e.ErrorMessage == "A senha deve ter pelo menos 6 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Does_Not_Match_Strength_Rules()
    {
        // arrange
        var command = new LoginUserCommand(
            UserName: "validUser",
            Password: "Senha123"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(LoginUserCommand.Password) &&
            e.ErrorMessage == "A senha deve conter pelo menos uma letra maiúscula, uma letra minúscula, um número e um caractere especial."));
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        var command = new LoginUserCommand(
            UserName: "validUser",
            Password: "Senha123!"
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
}