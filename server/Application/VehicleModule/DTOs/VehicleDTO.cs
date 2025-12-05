using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Application.VehicleModule.DTOs;
public record VehicleDTO(
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