using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Api.Models.VehicleModule;

public record UpdateVehicleResponse(
    bool UpdatedSuccessfully,
    string LicensePlate,
    string Brand,
    string Model,
    string Color,
    FuelType FuelType,
    decimal FuelTankCapacityInLiters,
    int Year,
    Guid VehicleGroupId,
    byte[] PhotoBytes
);
