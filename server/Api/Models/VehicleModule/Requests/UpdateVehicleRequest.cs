using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Api.Models.VehicleModule.Requests;

public record UpdateVehicleRequest(
    string Brand,
    string Model,
    string Color,
    FuelType FuelType,
    decimal FuelTankCapacityInLiters,
    int Year,
    Guid VehicleGroupId,
    byte[]? PhotoBytes
);