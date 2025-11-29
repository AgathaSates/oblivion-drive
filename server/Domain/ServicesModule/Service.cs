using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.ServicesModule;

public class Service : TenantEntity<Service>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public ChargeType ChargeType { get; set; }

    [ExcludeFromCodeCoverage]
    private Service() { }

    public Service(string name, decimal price, ChargeType chargeType, Guid companyId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Price = price;
        ChargeType = chargeType;
        CompanyId = companyId;
    }

    public override void Update(Service updatedEntity)
    {
        Name = updatedEntity.Name;
        Price = updatedEntity.Price;
        ChargeType = updatedEntity.ChargeType;
    }
}