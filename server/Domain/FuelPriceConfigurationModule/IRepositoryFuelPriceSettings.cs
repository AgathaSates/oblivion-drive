namespace OblivionDrive.Domain.FuelPriceConfigurationModule;

public interface IRepositoryFuelPriceSettings
{
    Task<FuelPriceConfiguration> GetAsync(Guid companyID);
    Task SaveAsync(FuelPriceConfiguration configuration, Guid companyId);
}