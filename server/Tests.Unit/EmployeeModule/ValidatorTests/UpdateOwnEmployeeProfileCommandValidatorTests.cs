using FluentValidation.Results;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.FluentValidation.Employee;

namespace OblivionDrive.Tests.Unit.EmployeeModule.ValidatorTests;

[TestClass]
[TestCategory("Employee - UpdateOwnEmployeeProfileCommandValidator Unit Tests")]
public class UpdateOwnEmployeeProfileCommandValidatorTests
{
    private UpdateOwnEmployeeProfileCommandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateOwnEmployeeProfileCommandValidator();
    }

    private static UpdateOwnEmployeeProfileCommand CreateValidCommand()
    {
        return new UpdateOwnEmployeeProfileCommand(
            Name: "Joao da Silva"
        );
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
            e.PropertyName == nameof(UpdateOwnEmployeeProfileCommand.Name) &&
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
            e.PropertyName == nameof(UpdateOwnEmployeeProfileCommand.Name) &&
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
            e.PropertyName == nameof(UpdateOwnEmployeeProfileCommand.Name) &&
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
            e.PropertyName == nameof(UpdateOwnEmployeeProfileCommand.Name) &&
            e.ErrorMessage == "O nome do funcionário deve conter apenas letras e espaços."));
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