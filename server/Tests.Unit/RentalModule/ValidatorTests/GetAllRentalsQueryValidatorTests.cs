using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Rental;
using OblivionDrive.Application.RentalModule.Querys;

namespace OblivionDrive.Tests.Unit.RentalModule.ValidatorTests;

[TestClass]
[TestCategory("Rental - GetAllRentalsQueryValidator Unit Tests")]
public class GetAllRentalsQueryValidatorTests
{
    private GetAllRentalsQueryValidator _validator = null!;

    private const string QuantityGreaterThanZeroMessage =
        "A quantidade deve ser maior que zero.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllRentalsQueryValidator();
    }

    private static GetAllRentalsQuery CreateValidQueryWithoutQuantity()
        => new GetAllRentalsQuery(Quantity: null);

    private static GetAllRentalsQuery CreateValidQueryWithQuantity(int quantity)
        => new GetAllRentalsQuery(Quantity: quantity);

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null()
    {
        // arrange
        GetAllRentalsQuery query = CreateValidQueryWithoutQuantity();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Within_Range()
    {
        // arrange
        GetAllRentalsQuery query = CreateValidQueryWithQuantity(10);

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
        GetAllRentalsQuery query = CreateValidQueryWithQuantity(0);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllRentalsQuery.Quantity) &&
            error.ErrorMessage == QuantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        // arrange
        GetAllRentalsQuery query = CreateValidQueryWithQuantity(-5);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllRentalsQuery.Quantity) &&
            error.ErrorMessage == QuantityGreaterThanZeroMessage));
    }
}
