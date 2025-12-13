using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Rental;
using OblivionDrive.Application.RentalModule.Querys;

namespace OblivionDrive.Tests.Unit.RentalModule.ValidatorTests;

[TestClass]
[TestCategory("Rental - GetRentalByIdQueryValidator Unit Tests")]
public class GetRentalByIdQueryValidatorTests
{
    private GetRentalByIdQueryValidator _validator = null!;

    private const string RentalIdRequiredMessage =
        "O identificador do aluguel é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetRentalByIdQueryValidator();
    }

    private static GetRentalByIdQuery CreateValidQuery()
        => new GetRentalByIdQuery(RentalId: Guid.NewGuid());

    [TestMethod]
    public void Should_Pass_When_RentalId_Is_Valid()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_RentalId_Is_Empty()
    {
        // arrange
        GetRentalByIdQuery query = CreateValidQuery() with { RentalId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetRentalByIdQuery.RentalId) &&
            error.ErrorMessage == RentalIdRequiredMessage));
    }
}