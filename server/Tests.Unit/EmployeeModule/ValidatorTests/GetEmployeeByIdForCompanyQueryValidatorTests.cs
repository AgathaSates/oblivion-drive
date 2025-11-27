using FluentValidation.Results;
using OblivionDrive.Application.EmployeeModule.Querys;
using OblivionDrive.Application.FluentValidation.Employee;

namespace OblivionDrive.Tests.Unit.EmployeeModule.ValidatorTests;

[TestClass]
[TestCategory("Employee - GetEmployeeByIdForCompanyQueryValidator Unit Tests")]
public class GetEmployeeByIdForCompanyQueryValidatorTests
{
    private GetEmployeeByIdForCompanyQueryValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetEmployeeByIdForCompanyQueryValidator();
    }

    private static GetEmployeeByIdForCompanyQuery CreateValidQuery()
    {
        return new GetEmployeeByIdForCompanyQuery(
            EmployeeId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Fail_When_EmployeeId_Is_Empty()
    {
        // arrange
        var query = CreateValidQuery() with { EmployeeId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetEmployeeByIdForCompanyQuery.EmployeeId) &&
            e.ErrorMessage == "O identificador do funcionário é obrigatório."));
    }

    [TestMethod]
    public void Should_Pass_When_Query_Is_Valid()
    {
        // arrange
        var query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
}
