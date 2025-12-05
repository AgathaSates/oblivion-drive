using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Api.Models.VehicleModule;

public record UpdateVehicleRequest(
    Guid VehicleId,
    string Brand,
    string Model,
    string Color,
    FuelType FuelType,
    decimal FuelTankCapacityInLiters,
    int Year,
    Guid VehicleGroupId,
    byte[]? PhotoBytes
);