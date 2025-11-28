namespace OblivionDrive.Api.Models.FuelPriceConfigurationModule;

public record UpdateFuelPriceConfigurationRequest(
    decimal Gasoline,
    decimal Gas,
    decimal Diesel,
    decimal Alcohol);