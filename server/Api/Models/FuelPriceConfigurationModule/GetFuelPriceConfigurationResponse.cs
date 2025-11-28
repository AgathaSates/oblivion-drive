namespace OblivionDrive.Api.Models.FuelPriceConfigurationModule;

public record GetFuelPriceConfigurationResponse(
    decimal Gasoline,
    decimal Gas,
    decimal Diesel,
    decimal Alcohol,
    DateOnly LastUpdate);