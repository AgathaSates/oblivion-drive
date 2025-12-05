using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Api.Models.VehicleModule.Responses;

public record RegisterVehicleResponse(
    bool CreatedSuccessfully,
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