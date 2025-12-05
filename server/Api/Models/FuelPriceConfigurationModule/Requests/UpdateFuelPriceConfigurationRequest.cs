namespace OblivionDrive.Api.Models.FuelPriceConfigurationModule.Requests;

public record UpdateFuelPriceConfigurationRequest(
    decimal Gasoline,
    decimal Gas,
    decimal Diesel,
    decimal Alcohol);