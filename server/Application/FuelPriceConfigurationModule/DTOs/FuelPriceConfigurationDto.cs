
namespace OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;
public record FuelPriceConfigurationDto(
    decimal Gasoline,
    decimal Gas,
    decimal Diesel,
    decimal Alcohol,
    DateOnly LastUpdate);
