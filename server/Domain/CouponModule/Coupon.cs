using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.CouponModule;
public class Coupon : TenantEntity<Coupon>
{
    public string Name { get; private set; }
    public decimal Value { get; private set; }
    public DateOnly ExpirationDate { get; private set; }

    public Guid PartnerId { get; private set; }
    public Partner Partner { get; private set; } = null!;

    private readonly HashSet<Guid> _usedByClientIds = new();
    public IReadOnlyCollection<Guid> UsedByClientIds => _usedByClientIds;

    [ExcludeFromCodeCoverage]
    private Coupon() { }

    public Coupon(
        string name, decimal value, DateOnly expirationDate, Guid partnerId, Guid companyId)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;

        Name = name;
        Value = value;
        ExpirationDate = expirationDate;
        PartnerId = partnerId;
    }

    public override void Update(Coupon updatedEntity)
    {
        Name = updatedEntity.Name;
        Value = updatedEntity.Value;
        ExpirationDate = updatedEntity.ExpirationDate;
        PartnerId = updatedEntity.PartnerId;
    }

    public bool IsExpired(DateOnly today) => ExpirationDate < today;

    public bool HasAlreadyBeenUsedBy(Guid clientId) => _usedByClientIds.Contains(clientId);

    public bool TryMarkAsUsedBy(Guid clientId)
    {
        return _usedByClientIds.Add(clientId);
    }
}