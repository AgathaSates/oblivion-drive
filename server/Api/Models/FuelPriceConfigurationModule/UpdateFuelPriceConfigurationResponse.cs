namespace OblivionDrive.Api.Models.FuelPriceConfigurationModule;

public record UpdateFuelPriceConfigurationResponse(
    decimal Gasoline,
    decimal Gas,
    decimal Diesel,
    decimal Alcohol,
    DateOnly LastUpdate);