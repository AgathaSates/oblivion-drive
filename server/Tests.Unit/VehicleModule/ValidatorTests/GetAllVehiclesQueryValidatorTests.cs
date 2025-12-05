using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Vehicle;
using OblivionDrive.Application.VehicleModule.Querys;

namespace OblivionDrive.Tests.Unit.VehicleModule.ValidatorTests;

[TestClass]
[TestCategory("Vehicle - GetAllVehiclesQueryValidator Unit Tests")]
public class GetAllVehiclesQueryValidatorTests
{
    private GetAllVehiclesQueryValidator _validator = null!;

    private const int MaximumQuantity = 1_000;

    private readonly string _quantityGreaterThanZeroMessage =
        "A quantidade deve ser maior que zero.";

    private readonly string _quantityMaxMessage =
        $"A quantidade não pode ser maior que {MaximumQuantity}.";

    private readonly string _vehicleGroupIdInvalidMessage =
        "O identificador do grupo de veículos, quando informado, não pode ser vazio.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetAllVehiclesQueryValidator();
    }

    private static GetAllVehiclesQuery CreateValidQueryWithoutFilters()
    {
        return new GetAllVehiclesQuery(
            Quantity: null,
            VehicleGroupId: null
        );
    }

    private static GetAllVehiclesQuery CreateValidQueryWithQuantity(int quantity)
    {
        return new GetAllVehiclesQuery(
            Quantity: quantity,
            VehicleGroupId: null
        );
    }

    private static GetAllVehiclesQuery CreateValidQueryWithVehicleGroupId(Guid? vehicleGroupId)
    {
        return new GetAllVehiclesQuery(
            Quantity: null,
            VehicleGroupId: vehicleGroupId
        );
    }

    [TestMethod]
    public void Should_Pass_When_Quantity_Is_Null_And_VehicleGroupId_Is_Null()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQueryWithoutFilters();

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
        GetAllVehiclesQuery query = CreateValidQueryWithQuantity(10);

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
        GetAllVehiclesQuery query = CreateValidQueryWithQuantity(MaximumQuantity);

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
        GetAllVehiclesQuery query = CreateValidQueryWithQuantity(0);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllVehiclesQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Negative()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQueryWithQuantity(-5);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllVehiclesQuery.Quantity) &&
            error.ErrorMessage == _quantityGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Quantity_Is_Greater_Than_Maximum()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQueryWithQuantity(MaximumQuantity + 1);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllVehiclesQuery.Quantity) &&
            error.ErrorMessage == _quantityMaxMessage));
    }

    [TestMethod]
    public void Should_Pass_When_VehicleGroupId_Is_Null()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQueryWithVehicleGroupId(null);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_VehicleGroupId_Is_Valid()
    {
        // arrange
        GetAllVehiclesQuery query = CreateValidQueryWithVehicleGroupId(Guid.NewGuid());

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
        GetAllVehiclesQuery query = CreateValidQueryWithVehicleGroupId(Guid.Empty);

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(GetAllVehiclesQuery.VehicleGroupId) &&
            error.ErrorMessage == _vehicleGroupIdInvalidMessage));
    }
}