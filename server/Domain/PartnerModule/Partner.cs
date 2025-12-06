using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.PartnerModule;
public class Partner : TenantEntity<Partner>
{
    public string Name { get; private set; }

    public ICollection<Coupon> Coupons { get; private set; } = new List<Coupon>();

    [ExcludeFromCodeCoverage]
    private Partner() { }

    public Partner(string name, Guid companyId)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;

        Name = name;
    }

    public override void Update(Partner updatedEntity)
    {
        Name = updatedEntity.Name;
    }
}