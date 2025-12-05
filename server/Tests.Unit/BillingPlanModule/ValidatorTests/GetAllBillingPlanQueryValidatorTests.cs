using FluentValidation.Results;
using OblivionDrive.Application.BillingPlanModule.Querys;
using OblivionDrive.Application.FluentValidation.BillingPlan;

namespace OblivionDrive.Tests.Unit.BillingPlanModule.ValidatorTests;

[TestClass]
[TestCategory("BillingPlan - GetAllBillingPlanQueryValidator Unit Tests")]
public class GetAllBillingPlanQueryValidatorTests
{
    private GetAllBillingPlanQueryValidator _validator = null!;

    private const int MaximumQuantity = 1_000;

    private readonly string _quantityGreaterThanZeroMessage =
        "A quantidade deve ser maior que zero.";

    private readonly string _quantityMaxMessage =
        $"A quantidade não pode ser maior que {MaximumQuantity}.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllBillingPlanQueryValidator();
    }

    private static GetAllBillingPlanQuery CreateValidQuery(int? quantity = null)
    {
        return new GetAllBillingPlanQuery(quantity);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null()
    {
        // arrange
        GetAllBillingPlanQuery query = CreateValidQuery(quantity: null);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Positive_And_Less_Than_Or_Equal_To_Maximum()
    {
        // arrange
        GetAllBillingPlanQuery query = CreateValidQuery(quantity: 10);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);

        GetAllBillingPlanQuery maxQuery = CreateValidQuery(quantity: MaximumQuantity);
        ValidationResult maxResult = _validator.Validate(maxQuery);

        Assert.IsTrue(maxResult.IsValid);
        Assert.AreEqual(0, maxResult.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        GetAllBillingPlanQuery zeroQuantityQuery = CreateValidQuery(quantity: 0);
        GetAllBillingPlanQuery negativeQuantityQuery = CreateValidQuery(quantity: -1);

        // act
        ValidationResult zeroResult = _validator.Validate(zeroQuantityQuery);
        ValidationResult negativeResult = _validator.Validate(negativeQuantityQuery);

        // assert
        Assert.IsFalse(zeroResult.IsValid);
        Assert.IsTrue(zeroResult.Errors.Any(e =>
            e.PropertyName == nameof(GetAllBillingPlanQuery.Quantity) &&
            e.ErrorMessage == _quantityGreaterThanZeroMessage));

        Assert.IsFalse(negativeResult.IsValid);
        Assert.IsTrue(negativeResult.Errors.Any(e =>
            e.PropertyName == nameof(GetAllBillingPlanQuery.Quantity) &&
            e.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Greater_Than_Maximum()
    {
        // arrange
        GetAllBillingPlanQuery query = CreateValidQuery(quantity: MaximumQuantity + 1);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetAllBillingPlanQuery.Quantity) &&
            e.ErrorMessage == _quantityMaxMessage));
    }
}
