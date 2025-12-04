using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.VehicleGroup;
using OblivionDrive.Application.VehicleGroupModule.Querys;

namespace OblivionDrive.Tests.Unit.VehicleGroupModule.ValidatorTests;

[TestClass]
[TestCategory("VehicleGroup - GetAllVehicleGroupQueryValidator Unit Tests")]
public class GetAllVehicleGroupQueryValidatorTests
{
    private GetAllVehicleGroupQueryValidator _validator = null!;

    private const int MaximumQuantity = 1_000;

    private readonly string _quantityGreaterThanZeroMessage =
        "A quantidade deve ser maior que zero.";

    private readonly string _quantityMaxMessage =
        $"A quantidade não pode ser maior que {MaximumQuantity}.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllVehicleGroupQueryValidator();
    }

    private static GetAllVehicleGroupQuery CreateValidQuery()
    {
        return new GetAllVehicleGroupQuery(
            Quantity: 10
        );
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null()
    {
        // arrange
        GetAllVehicleGroupQuery query = new GetAllVehicleGroupQuery(
            Quantity: null
        );

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Within_Allowed_Range()
    {
        // arrange
        GetAllVehicleGroupQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Less_Than_Or_Equal_To_Zero()
    {
        // arrange
        GetAllVehicleGroupQuery query = new GetAllVehicleGroupQuery(
            Quantity: 0
        );

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetAllVehicleGroupQuery.Quantity) &&
            e.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Greater_Than_Maximum()
    {
        // arrange
        GetAllVehicleGroupQuery query = new GetAllVehicleGroupQuery(
            Quantity: MaximumQuantity + 1
        );

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetAllVehicleGroupQuery.Quantity) &&
            e.ErrorMessage == _quantityMaxMessage));
    }
}