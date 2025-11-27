using FluentValidation.Results;
using OblivionDrive.Application.EmployeeModule.Querys;
using OblivionDrive.Application.FluentValidation.Employee;

namespace OblivionDrive.Tests.Unit.EmployeeModule.ValidatorTests;

[TestClass]
[TestCategory("Employee - GetAllEmployeesForCompanyQueryValidator Unit Tests")]
public sealed class GetAllEmployeesForCompanyQueryValidatorTests
{
    private GetAllEmployeesForCompanyQueryValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllEmployeesForCompanyQueryValidator();
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null()
    {
        // arrange
        var query = new GetAllEmployeesForCompanyQuery(Quantity: null);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Positive()
    {
        // arrange
        var query = new GetAllEmployeesForCompanyQuery(Quantity: 5);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Zero()
    {
        // arrange
        var query = new GetAllEmployeesForCompanyQuery(Quantity: 0);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetAllEmployeesForCompanyQuery.Quantity) &&
            e.ErrorMessage == "A quantidade deve ser maior que zero."));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        // arrange
        var query = new GetAllEmployeesForCompanyQuery(Quantity: -1);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetAllEmployeesForCompanyQuery.Quantity) &&
            e.ErrorMessage == "A quantidade deve ser maior que zero."));
    }
}