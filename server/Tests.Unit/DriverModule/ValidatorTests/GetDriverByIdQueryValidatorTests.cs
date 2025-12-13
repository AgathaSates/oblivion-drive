using FluentValidation.Results;
using OblivionDrive.Application.DriverModule.Querys;
using OblivionDrive.Application.FluentValidation.Driver;

namespace OblivionDrive.Tests.Unit.DriverModule.ValidatorTests;

[TestClass]
[TestCategory("Driver - GetDriverByIdQueryValidator Unit Tests")]
public class GetDriverByIdQueryValidatorTests
{
    private GetDriverByIdQueryValidator _validator = null!;

    private const string DriverIdRequiredMessage =
        "O identificador do condutor é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetDriverByIdQueryValidator();
    }

    private static GetDriverByIdQuery CreateValidQuery()
    {
        return new GetDriverByIdQuery(
            DriverId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_DriverId_Is_Valid()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_DriverId_Is_Empty()
    {
        // arrange
        GetDriverByIdQuery query = CreateValidQuery() with { DriverId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetDriverByIdQuery.DriverId) &&
            error.ErrorMessage == DriverIdRequiredMessage));
    }
}