using FluentValidation.Results;
using OblivionDrive.Application.CouponModule.Querys;
using OblivionDrive.Application.FluentValidation.Coupon;

namespace OblivionDrive.Tests.Unit.CouponModule.ValidatorTests;

[TestClass]
[TestCategory("Coupon - GetAllCouponsQueryValidator Unit Tests")]
public sealed class GetAllCouponsQueryValidatorTests
{
    private GetAllCouponsQueryValidator _validator = null!;
    private const int MaximumQuantity = 1_000;
    private readonly string _quantityGreaterThanZeroMessage = "A quantidade deve ser maior que zero.";
    private readonly string _quantityMaxMessage = $"A quantidade não pode ser maior que {MaximumQuantity}.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllCouponsQueryValidator();
    }

    private static GetAllCouponsQuery CreateValidQueryWithoutQuantity()
    {
        return new GetAllCouponsQuery(Quantity: null);
    }

    private static GetAllCouponsQuery CreateValidQueryWithQuantity(int quantity)
    {
        return new GetAllCouponsQuery(Quantity: quantity);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null()
    {
        // arrange
        GetAllCouponsQuery query = CreateValidQueryWithoutQuantity();

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
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(10);

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
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(MaximumQuantity);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_One()
    {
        // arrange
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(1);

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
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(0);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllCouponsQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        // arrange
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(-5);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllCouponsQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Greater_Than_Maximum()
    {
        // arrange
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(MaximumQuantity + 1);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllCouponsQuery.Quantity) &&
            error.ErrorMessage == _quantityMaxMessage));
    }

    [TestMethod]
    public void Should_Have_Exactly_One_Error_When_Quantity_Is_Zero()
    {
        // arrange
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(0);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(1, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Have_Exactly_One_Error_When_Quantity_Is_Greater_Than_Maximum()
    {
        // arrange
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(MaximumQuantity + 100);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(1, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Maximum_Minus_One()
    {
        // arrange
        GetAllCouponsQuery query = CreateValidQueryWithQuantity(MaximumQuantity - 1);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
}
