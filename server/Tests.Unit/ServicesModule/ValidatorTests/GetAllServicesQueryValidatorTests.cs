using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Services;
using OblivionDrive.Application.ServicesModule.Querys;

namespace OblivionDrive.Tests.Unit.ServicesModule.ValidatorTests;

[TestClass]
[TestCategory("Service - GetAllServicesQueryValidator Unit Tests")]
public class GetAllServicesQueryValidatorTests
{
    private GetAllServicesQueryValidator _validator = null!;

    private const int MaximumQuantity = 1_000;

    private readonly string _quantityGreaterThanZeroMessage =
        "A quantidade deve ser maior que zero.";

    private readonly string _quantityMaxMessage =
        $"A quantidade não pode ser maior que {MaximumQuantity}.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllServicesQueryValidator();
    }

    private static GetAllServicesQuery CreateValidQueryWithoutQuantity()
    {
        return new GetAllServicesQuery(Quantity: null);
    }

    private static GetAllServicesQuery CreateValidQueryWithQuantity(int quantity)
    {
        return new GetAllServicesQuery(Quantity: quantity);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null()
    {
        // arrange
        GetAllServicesQuery query = CreateValidQueryWithoutQuantity();

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
        GetAllServicesQuery query = CreateValidQueryWithQuantity(10);

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
        GetAllServicesQuery query = CreateValidQueryWithQuantity(MaximumQuantity);

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
        GetAllServicesQuery query = CreateValidQueryWithQuantity(0);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllServicesQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        // arrange
        GetAllServicesQuery query = CreateValidQueryWithQuantity(-5);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllServicesQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Greater_Than_Maximum()
    {
        // arrange
        GetAllServicesQuery query = CreateValidQueryWithQuantity(MaximumQuantity + 1);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllServicesQuery.Quantity) &&
            error.ErrorMessage == _quantityMaxMessage));
    }
}