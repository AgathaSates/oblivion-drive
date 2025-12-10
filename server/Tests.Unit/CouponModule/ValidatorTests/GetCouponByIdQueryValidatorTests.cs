using FluentValidation.Results;
using OblivionDrive.Application.CouponModule.Querys;
using OblivionDrive.Application.FluentValidation.Coupon;

namespace OblivionDrive.Tests.Unit.CouponModule.ValidatorTests;

[TestClass]
[TestCategory("Coupon - GetCouponByIdQueryValidator Unit Tests")]
public sealed class GetCouponByIdQueryValidatorTests
{
    private GetCouponByIdQueryValidator _validator = null!;

    private const string CouponIdRequiredMessage = "O identificador do cupom é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetCouponByIdQueryValidator();
    }

    private static GetCouponByIdQuery CreateValidQuery()
    {
        return new GetCouponByIdQuery(
            CouponId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_CouponId_Is_Valid()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_CouponId_Is_Empty()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery() with { CouponId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetCouponByIdQuery.CouponId) &&
            error.ErrorMessage == CouponIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Have_Exactly_One_Error_When_CouponId_Is_Empty()
    {
        // arrange
        GetCouponByIdQuery query = CreateValidQuery() with { CouponId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(1, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_CouponId_Is_Valid_Guid()
    {
        // arrange
        Guid validCouponId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        GetCouponByIdQuery query = new GetCouponByIdQuery(CouponId: validCouponId);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_With_Correct_Property_Name_When_CouponId_Is_Empty()
    {
        // arrange
        GetCouponByIdQuery query = new GetCouponByIdQuery(CouponId: Guid.Empty);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.All(e => e.PropertyName == nameof(GetCouponByIdQuery.CouponId)));
    }
}
