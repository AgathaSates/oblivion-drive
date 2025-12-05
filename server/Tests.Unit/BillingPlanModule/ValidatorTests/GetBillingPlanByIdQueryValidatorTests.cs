using FluentValidation.Results;
using OblivionDrive.Application.BillingPlanModule.Querys;
using OblivionDrive.Application.FluentValidation.BillingPlan;

namespace OblivionDrive.Tests.Unit.BillingPlanModule.ValidatorTests;

[TestClass]
[TestCategory("BillingPlan - GetBillingPlanByIdQueryValidator Unit Tests")]
public class GetBillingPlanByIdQueryValidatorTests
{
    private GetBillingPlanByIdQueryValidator _validator = null!;

    private readonly string _billingPlanIdRequiredMessage =
        "O identificador do plano de cobrança é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetBillingPlanByIdQueryValidator();
    }

    private static GetBillingPlanByIdQuery CreateValidQuery()
    {
        return new GetBillingPlanByIdQuery(
            BillingPlanId: Guid.NewGuid());
    }

    [TestMethod]
    public void Should_Pass_When_Query_Is_Valid()
    {
        // arrange
        GetBillingPlanByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_BillingPlanId_Is_Empty()
    {
        // arrange
        GetBillingPlanByIdQuery query = new GetBillingPlanByIdQuery(
            BillingPlanId: Guid.Empty);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetBillingPlanByIdQuery.BillingPlanId) &&
            e.ErrorMessage == _billingPlanIdRequiredMessage));
    }
}