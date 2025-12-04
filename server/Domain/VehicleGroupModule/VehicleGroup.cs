using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.VehicleGroupModule;

public class VehicleGroup : TenantEntity<VehicleGroup>
{
    public string Name { get; private set; }
    public ICollection<BillingPlan> BillingPlans { get; private set; } = new List<BillingPlan>();


    [ExcludeFromCodeCoverage]
    private VehicleGroup() { }

    public VehicleGroup(string name, Guid companyId)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Name = name;
    }

    public override void Update(VehicleGroup updatedEntity)
    {
        Name = updatedEntity.Name;
    }
}