
using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Partner;
using OblivionDrive.Application.PartnerModule.Querys;

namespace OblivionDrive.Tests.Unit.PartnerModule.ValidatorTests;

[TestClass]
[TestCategory("Partner - GetAllPartnersQueryValidator Unit Tests")]
public class GetAllPartnersQueryValidatorTests
{
    private GetAllPartnersQueryValidator _validator = null!;

    private const int MaximumQuantity = 1_000;

    private readonly string _quantityGreaterThanZeroMessage =
        "A quantidade deve ser maior que zero.";

    private readonly string _quantityMaxMessage =
        $"A quantidade não pode ser maior que {MaximumQuantity}.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllPartnersQueryValidator();
    }

    private static GetAllPartnersQuery CreateValidQueryWithoutQuantity()
    {
        return new GetAllPartnersQuery(Quantity: null);
    }

    private static GetAllPartnersQuery CreateValidQueryWithQuantity(int quantity)
    {
        return new GetAllPartnersQuery(Quantity: quantity);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null()
    {
        // arrange
        GetAllPartnersQuery query = CreateValidQueryWithoutQuantity();

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
        GetAllPartnersQuery query = CreateValidQueryWithQuantity(10);

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
        GetAllPartnersQuery query = CreateValidQueryWithQuantity(MaximumQuantity);

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
        GetAllPartnersQuery query = CreateValidQueryWithQuantity(0);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllPartnersQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        // arrange
        GetAllPartnersQuery query = CreateValidQueryWithQuantity(-5);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllPartnersQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Greater_Than_Maximum()
    {
        // arrange
        GetAllPartnersQuery query = CreateValidQueryWithQuantity(MaximumQuantity + 1);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllPartnersQuery.Quantity) &&
            error.ErrorMessage == _quantityMaxMessage));
    }
}