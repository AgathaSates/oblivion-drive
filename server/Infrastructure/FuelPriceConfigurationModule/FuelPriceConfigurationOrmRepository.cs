using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.FuelPriceConfigurationModule;

public class FuelPriceConfigurationOrmRepository(OblivionDriveDbContext context) : IRepositoryFuelPriceSettings
{
    public async Task<FuelPriceConfiguration> GetAsync(Guid companyId)
    {
        var existingConfiguration = await context
           .Set<FuelPriceConfiguration>()
           .SingleOrDefaultAsync(configuration => configuration.CompanyId == companyId);

        if (existingConfiguration is not null)
            return existingConfiguration;

        var newConfiguration = new FuelPriceConfiguration(
            gasoline: 0m,
            gas: 0m,
            diesel: 0m,
            alcohol: 0m,
            companyId: companyId);

        context.Add(newConfiguration);

        await context.SaveChangesAsync();

        return newConfiguration;
    }

    public async Task SaveAsync(FuelPriceConfiguration configuration, Guid companyId)
    {
        var currentConfiguration = await GetAsync(companyId);

        currentConfiguration.Update(configuration);
    }
}