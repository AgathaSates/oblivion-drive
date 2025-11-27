using FluentValidation.Results;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.FluentValidation.Employee;

namespace OblivionDrive.Tests.Unit.EmployeeModule.ValidatorTests;

[TestClass]
[TestCategory("Employee - UpdateEmployeeByCompanyCommandValidator Unit Tests")]
public class UpdateEmployeeByCompanyCommandValidatorTests
{
    private UpdateEmployeeByCompanyCommandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateEmployeeByCompanyCommandValidator();
    }
    private static UpdateEmployeeByCompanyCommand CreateValidCommand()
    {
        return new UpdateEmployeeByCompanyCommand(
            EmployeeId: Guid.NewGuid(),
            Name: "Joao da Silva",
            HireDate: DateOnly.FromDateTime(DateTime.Today),
            Salary: 3000m
        );
    }

    [TestMethod]
    public void Should_Fail_When_EmployeeId_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { EmployeeId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.EmployeeId) &&
            e.ErrorMessage == "O identificador do funcionário é obrigatório."));
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.Name) &&
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.Name) &&
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.Name) &&
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.Name) &&
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.HireDate) &&
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.HireDate) &&
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.HireDate) &&
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.Salary) &&
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
            e.PropertyName == nameof(UpdateEmployeeByCompanyCommand.Salary) &&
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