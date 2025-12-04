using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.VehicleGroup;
using OblivionDrive.Application.VehicleGroupModule.Querys;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule.ValidatorTests;

[TestClass]
[TestCategory("VehicleGroup - GetVehicleGroupByIdQueryValidator Unit Tests")]
public class GetVehicleGroupByIdQueryValidatorTests
{
    private GetVehicleGroupByIdQueryValidator _validator = null!;

    private readonly string _vehicleGroupIdRequiredMessage =
        "O identificador do grupo de veículos é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetVehicleGroupByIdQueryValidator();
    }

    private static GetVehicleGroupByIdQuery CreateValidQuery()
    {
        return new GetVehicleGroupByIdQuery(
            VehicleGroupId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_Query_Is_Valid()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_VehicleGroupId_Is_Empty()
    {
        // arrange
        GetVehicleGroupByIdQuery query = CreateValidQuery() with { VehicleGroupId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetVehicleGroupByIdQuery.VehicleGroupId) &&
            e.ErrorMessage == _vehicleGroupIdRequiredMessage));
    }
}