using FluentValidation.Results;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.FluentValidation.Employee;

namespace OblivionDrive.Tests.Unit.EmployeeModule.ValidatorTests;
[TestClass]
[TestCategory("Employee - DeleteEmployeeByCompanyCommandValidator Unit Tests")]
public sealed class DeleteEmployeeByCompanyCommandValidatorTests
{
    private DeleteEmployeeByCompanyCommandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteEmployeeByCompanyCommandValidator();
    }

    private static DeleteEmployeeByCompanyCommand CreateValidCommand()
    {
        return new DeleteEmployeeByCompanyCommand(
            EmployeeId: Guid.NewGuid()
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
            e.PropertyName == nameof(DeleteEmployeeByCompanyCommand.EmployeeId) &&
            e.ErrorMessage == "O identificador do funcionário é obrigatório."));
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