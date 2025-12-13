using FluentValidation.Results;
using OblivionDrive.Application.DriverModule.Querys;
using OblivionDrive.Application.FluentValidation.Driver;

namespace OblivionDrive.Tests.Unit.DriverModule.ValidatorTests;

[TestClass]
[TestCategory("Driver - GetAllDriversQueryValidator Unit Tests")]
public class GetAllDriversQueryValidatorTests
{
    private GetAllDriversQueryValidator _validator = null!;

    private const int MaximumQuantity = 1_000;

    private readonly string _quantityGreaterThanZeroMessage =
        "A quantidade deve ser maior que zero.";

    private readonly string _quantityMaxMessage =
        $"A quantidade não pode ser maior que {MaximumQuantity}.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllDriversQueryValidator();
    }

    private static GetAllDriversQuery CreateValidQueryWithoutQuantity()
    {
        return new GetAllDriversQuery(Quantity: null);
    }

    private static GetAllDriversQuery CreateValidQueryWithQuantity(int quantity)
    {
        return new GetAllDriversQuery(Quantity: quantity);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null()
    {
        // arrange
        GetAllDriversQuery query = CreateValidQueryWithoutQuantity();

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
        GetAllDriversQuery query = CreateValidQueryWithQuantity(10);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Equal_To_Maximum()
    {
        // arrange
        GetAllDriversQuery query = CreateValidQueryWithQuantity(MaximumQuantity);

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
        GetAllDriversQuery query = CreateValidQueryWithQuantity(0);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllDriversQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        // arrange
        GetAllDriversQuery query = CreateValidQueryWithQuantity(-5);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllDriversQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Greater_Than_Maximum()
    {
        // arrange
        GetAllDriversQuery query = CreateValidQueryWithQuantity(MaximumQuantity + 1);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllDriversQuery.Quantity) &&
            error.ErrorMessage == _quantityMaxMessage));
    }
}