using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Tests.Unit.VehicleModule;

[TestClass]
[TestCategory("Vehicle - Entity Unit Tests")]
public class VehicleTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        string licensePlate = "ABC1D23";
        string brand = "Toyota";
        string model = "Corolla";
        string color = "White";
        FuelType fuelType = (FuelType)1;
        decimal fuelTankCapacityInLiters = 55.5m;
        int year = 2024;
        Guid vehicleGroupId = Guid.NewGuid();
        Guid companyId = Guid.NewGuid();

        // act
        Vehicle vehicle = new Vehicle(
            licensePlate: licensePlate,
            brand: brand,
            model: model,
            color: color,
            fuelType: fuelType,
            fuelTankCapacityInLiters: fuelTankCapacityInLiters,
            year: year,
            vehicleGroupId: vehicleGroupId,
            companyId: companyId);

        // assert
        Assert.AreNotEqual(Guid.Empty, vehicle.Id);
        Assert.AreEqual(companyId, vehicle.CompanyId);

        Assert.AreEqual(licensePlate, vehicle.LicensePlate);
        Assert.AreEqual(brand, vehicle.Brand);
        Assert.AreEqual(model, vehicle.Model);
        Assert.AreEqual(color, vehicle.Color);

        Assert.AreEqual(fuelType, vehicle.FuelType);
        Assert.AreEqual(fuelTankCapacityInLiters, vehicle.FuelTankCapacityInLiters);
        Assert.AreEqual(year, vehicle.Year);

        Assert.AreEqual(vehicleGroupId, vehicle.VehicleGroupId);

        Assert.IsNull(vehicle.PhotoBytes);
    }

    [TestMethod]
    public void Update_Should_Update_Properties_And_Keep_Id_CompanyId_And_LicensePlate()
    {
        // arrange
        Guid companyId = Guid.NewGuid();
        Guid originalVehicleGroupId = Guid.NewGuid();

        string originalLicensePlate = "ABC1D23";

        Vehicle originalVehicle = new Vehicle(
            licensePlate: originalLicensePlate,
            brand: "Original brand",
            model: "Original model",
            color: "Original color",
            fuelType: (FuelType)1,
            fuelTankCapacityInLiters: 50.0m,
            year: 2022,
            vehicleGroupId: originalVehicleGroupId,
            companyId: companyId);

        Guid originalId = originalVehicle.Id;
        Guid originalCompanyId = originalVehicle.CompanyId;
        string originalLicensePlateSnapshot = originalVehicle.LicensePlate;

        byte[] originalPhotoBytes = new byte[] { 1, 2, 3 };
        originalVehicle.SetPhoto(originalPhotoBytes);

        Guid updatedVehicleGroupId = Guid.NewGuid();

        Vehicle updatedVehicle = new Vehicle(
            licensePlate: "DIFFERENT-PLATE",
            brand: "Updated brand",
            model: "Updated model",
            color: "Updated color",
            fuelType: (FuelType)2,
            fuelTankCapacityInLiters: 60.0m,
            year: 2023,
            vehicleGroupId: updatedVehicleGroupId,
            companyId: Guid.NewGuid());

        byte[] updatedPhotoBytes = new byte[] { 4, 5, 6 };
        updatedVehicle.SetPhoto(updatedPhotoBytes);

        // act
        originalVehicle.Update(updatedVehicle);

        // assert
        Assert.AreEqual(updatedVehicle.Brand, originalVehicle.Brand);
        Assert.AreEqual(updatedVehicle.Model, originalVehicle.Model);
        Assert.AreEqual(updatedVehicle.Color, originalVehicle.Color);
        Assert.AreEqual(updatedVehicle.FuelType, originalVehicle.FuelType);
        Assert.AreEqual(updatedVehicle.FuelTankCapacityInLiters, originalVehicle.FuelTankCapacityInLiters);
        Assert.AreEqual(updatedVehicle.Year, originalVehicle.Year);
        Assert.AreEqual(updatedVehicle.VehicleGroupId, originalVehicle.VehicleGroupId);

        CollectionAssert.AreEqual(updatedPhotoBytes, originalVehicle.PhotoBytes);

        Assert.AreEqual(originalId, originalVehicle.Id);
        Assert.AreEqual(originalCompanyId, originalVehicle.CompanyId);
        Assert.AreEqual(originalLicensePlateSnapshot, originalVehicle.LicensePlate);
    }

    [TestMethod]
    public void SetPhoto_Should_Update_PhotoBytes()
    {
        // arrange
        Vehicle vehicle = new Vehicle(
            licensePlate: "ABC1D23",
            brand: "Toyota",
            model: "Corolla",
            color: "White",
            fuelType: (FuelType)1,
            fuelTankCapacityInLiters: 55.5m,
            year: 2024,
            vehicleGroupId: Guid.NewGuid(),
            companyId: Guid.NewGuid());

        byte[] expectedPhotoBytes = new byte[] { 10, 20, 30, 40 };

        // act
        vehicle.SetPhoto(expectedPhotoBytes);

        // assert
        CollectionAssert.AreEqual(expectedPhotoBytes, vehicle.PhotoBytes);
    }
}