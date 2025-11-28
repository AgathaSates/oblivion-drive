using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.FuelPriceConfigurationModule;
public class FuelPriceConfiguration : TenantEntity<FuelPriceConfiguration>
{
    public decimal Gasoline { get; private set; }
    public decimal Gas { get; private set; }
    public decimal Diesel{ get; private set; }
    public decimal Alcohol { get; private set; }
    public DateOnly LastUpdate { get; private set; }

    [ExcludeFromCodeCoverage]
    private FuelPriceConfiguration() { }

    public FuelPriceConfiguration(decimal gasoline, decimal gas, decimal diesel, decimal alcohol, Guid companyId)
    {
        Id = Guid.NewGuid();
        Gasoline = gasoline;
        Gas = gas;
        Diesel = diesel;
        Alcohol = alcohol;
        LastUpdate = DateOnly.FromDateTime(DateTime.Now);
        CompanyId = companyId;
    }

    public override void Update(FuelPriceConfiguration updatedEntity)
    {
        Gasoline = updatedEntity.Gasoline;
        Gas = updatedEntity.Gas;
        Diesel = updatedEntity.Diesel;
        Alcohol = updatedEntity.Alcohol;
        LastUpdate = DateOnly.FromDateTime(DateTime.Now);
    }
}