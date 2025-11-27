using FluentValidation.Results;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.FluentValidation.Employee;

namespace OblivionDrive.Tests.Unit.EmployeeModule.ValidatorTests;

[TestClass]
[TestCategory("Employee - RegisterEmployeeComandValidator Unit Tests")]
public class RegisterEmployeeComandValidatorTests
{
    private RegisterEmployeeComandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new RegisterEmployeeComandValidator();
    }
    private static RegisterEmployeeCommand CreateValidCommand()
    {
        return new RegisterEmployeeCommand(
            UserName: "validUser",
            Email: "user@example.com",
            Password: "Senha123!",
            Name: "Joao da Silva",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
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
            e.PropertyName == nameof(RegisterEmployeeCommand.UserName) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.UserName) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.UserName) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.UserName) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.Email) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.Email) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.Email) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.Password) &&
            e.ErrorMessage == "A senha é obrigatória."));
    }

    [TestMethod]
    public void Should_Fail_When_Password_Is_Too_Short()
    {
        // arrange
        var command = CreateValidCommand() with { Password = "Ab1!" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.Password) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.Password) &&
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
            e.PropertyName == nameof(RegisterEmployeeCommand.Password) &&
            e.ErrorMessage == "A senha deve conter pelo menos uma letra maiúscula, uma letra minúscula, um número e um caractere especial."));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { Name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.Name) &&
            e.ErrorMessage == "O nome do funcionário é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        var command = CreateValidCommand() with { Name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.Name) &&
            e.ErrorMessage == "O nome do funcionário deve ter pelo menos 2 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        var longName = new string('A', 201);
        var command = CreateValidCommand() with { Name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.Name) &&
            e.ErrorMessage == "O nome do funcionário deve ter no máximo 200 caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Has_Invalid_Characters()
    {
        // arrange
        var command = CreateValidCommand() with { Name = "Joao 123" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.Name) &&
            e.ErrorMessage == "O nome do funcionário deve conter apenas letras e espaços."));
    }

    [TestMethod]
    public void Should_Fail_When_HireDate_Is_Default()
    {
        // arrange
        var command = CreateValidCommand() with { HireDate = default };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.HireDate) &&
            e.ErrorMessage == "A data de contratação é obrigatória."));
    }

    [TestMethod]
    public void Should_Fail_When_HireDate_Is_Before_Minimum()
    {
        // arrange
        var command = CreateValidCommand() with
        {
            HireDate = new DateOnly(1969, 12, 31)
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.HireDate) &&
            e.ErrorMessage == "A data de contratação não pode ser anterior a 01/01/1970."));
    }

    [TestMethod]
    public void Should_Fail_When_HireDate_Is_Future()
    {
        // arrange
        var command = CreateValidCommand() with
        {
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.HireDate) &&
            e.ErrorMessage == "A data de contratação não pode ser uma data futura."));
    }

    [TestMethod]
    public void Should_Fail_When_Salary_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        var command = CreateValidCommand() with { Salary = 0m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.Salary) &&
            e.ErrorMessage == "O salário deve ser maior que zero."));
    }

    [TestMethod]
    public void Should_Fail_When_Salary_Is_Greater_Than_Maximum()
    {
        // arrange
        var command = CreateValidCommand() with { Salary = 1_000_000m + 0.01m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(RegisterEmployeeCommand.Salary) &&
            e.ErrorMessage == "O salário não pode ser maior que 1.000.000,00."));
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