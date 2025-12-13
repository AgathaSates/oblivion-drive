using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Partner;
using OblivionDrive.Application.PartnerModule.Querys;

namespace OblivionDrive.Tests.Unit.PartnerModule.ValidatorTests;

[TestClass]
[TestCategory("Partner - GetPartnerByIdQueryValidator Unit Tests")]
public class GetPartnerByIdQueryValidatorTests
{
    private GetPartnerByIdQueryValidator _validator = null!;

    private const string PartnerIdRequiredMessage =
        "O identificador do parceiro é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetPartnerByIdQueryValidator();
    }

    private static GetPartnerByIdQuery CreateValidQuery()
    {
        return new GetPartnerByIdQuery(
            PartnerId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_PartnerId_Is_Valid()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_PartnerId_Is_Empty()
    {
        // arrange
        GetPartnerByIdQuery query = CreateValidQuery() with { PartnerId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetPartnerByIdQuery.PartnerId) &&
            error.ErrorMessage == PartnerIdRequiredMessage));
    }
}