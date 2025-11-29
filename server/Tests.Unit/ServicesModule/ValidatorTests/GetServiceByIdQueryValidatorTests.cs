
using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Services;
using OblivionDrive.Application.ServicesModule.Querys;

namespace OblivionDrive.Tests.Unit.ServicesModule.ValidatorTests;

[TestClass]
[TestCategory("Service - GetServiceByIdQueryValidator Unit Tests")]
public class GetServiceByIdQueryValidatorTests
{
    private GetServiceByIdQueryValidator _validator = null!;

    private const string ServiceIdRequiredMessage =
        "O identificador do serviço é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetServiceByIdQueryValidator();
    }

    private static GetServiceByIdQuery CreateValidQuery()
    {
        return new GetServiceByIdQuery(
            ServiceId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_ServiceId_Is_Valid()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_ServiceId_Is_Empty()
    {
        // arrange
        GetServiceByIdQuery query = CreateValidQuery() with { ServiceId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetServiceByIdQuery.ServiceId) &&
            error.ErrorMessage == ServiceIdRequiredMessage));
    }
}