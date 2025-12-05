using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Domain.VehicleModule;
public class Vehicle : TenantEntity<Vehicle>
{
    public string LicensePlate { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public string Color { get; private set; }

    public FuelType FuelType { get; private set; }
    public decimal FuelTankCapacityInLiters { get; private set; }
    public int Year { get; private set; }

    public Guid VehicleGroupId { get; private set; }
    public VehicleGroup VehicleGroup { get; private set; } = null!;

    public byte[]? PhotoBytes { get; private set; }

    [ExcludeFromCodeCoverage]
    private Vehicle() { }

    public Vehicle(
        string licensePlate, string brand, string model, string color, FuelType fuelType,
        decimal fuelTankCapacityInLiters, int year, Guid vehicleGroupId, Guid companyId)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;

        LicensePlate = licensePlate;
        Brand = brand;
        Model = model;
        Color = color;
        FuelType = fuelType;
        FuelTankCapacityInLiters = fuelTankCapacityInLiters;
        Year = year;
        VehicleGroupId = vehicleGroupId;
    }

    public override void Update(Vehicle updatedEntity)
    {
        Brand = updatedEntity.Brand;
        Model = updatedEntity.Model;
        Color = updatedEntity.Color;
        FuelType = updatedEntity.FuelType;
        FuelTankCapacityInLiters = updatedEntity.FuelTankCapacityInLiters;
        Year = updatedEntity.Year;
        VehicleGroupId = updatedEntity.VehicleGroupId;

        PhotoBytes = updatedEntity.PhotoBytes;
    }

    public void SetPhoto(byte[] photoBytes)
    {
        PhotoBytes = photoBytes;
    }
}