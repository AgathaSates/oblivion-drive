using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Vehicle;
using OblivionDrive.Application.VehicleModule.Querys;

namespace OblivionDrive.Tests.Unit.VehicleModule.ValidatorTests;

[TestClass]
[TestCategory("Vehicle - GetVehicleByIdQueryValidator Unit Tests")]
public class GetVehicleByIdQueryValidatorTests
{
    private GetVehicleByIdQueryValidator _validator = null!;

    private const string VehicleIdRequiredMessage =
        "O identificador do veículo é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetVehicleByIdQueryValidator();
    }

    private static GetVehicleByIdQuery CreateValidQuery()
    {
        return new GetVehicleByIdQuery(
            VehicleId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_VehicleId_Is_Valid()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_VehicleId_Is_Empty()
    {
        // arrange
        GetVehicleByIdQuery query = CreateValidQuery() with { VehicleId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetVehicleByIdQuery.VehicleId) &&
            error.ErrorMessage == VehicleIdRequiredMessage));
    }
}