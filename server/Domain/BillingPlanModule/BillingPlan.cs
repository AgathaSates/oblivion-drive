using System.Diagnostics.CodeAnalysis;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Domain.BillingPlanModule;
public class BillingPlan : TenantEntity<BillingPlan>
{
    public string Name { get; private set; }

    public Guid VehicleGroupId { get; private set; }
    public VehicleGroup VehicleGroup { get; private set; }

    public DailyBillingPlanConfig DailyPlan { get; private set; }
    public ControlledBillingPlanConfig ControlledPlan { get; private set; }
    public FreeBillingPlanConfig FreePlan { get; private set; }

    [ExcludeFromCodeCoverage]
    private BillingPlan() { }

    public BillingPlan(
        string name,
        Guid companyId,
        Guid vehicleGroupId,
        DailyBillingPlanConfig dailyPlan,
        ControlledBillingPlanConfig controlledPlan,
        FreeBillingPlanConfig freePlan)
    {

        Id = Guid.NewGuid();
        CompanyId = companyId;
        VehicleGroupId = vehicleGroupId;

        Name = name;
        DailyPlan = dailyPlan;
        ControlledPlan = controlledPlan;
        FreePlan = freePlan;
    }

    public override void Update(BillingPlan updatedEntity)
    {
        Name = updatedEntity.Name;
        DailyPlan = updatedEntity.DailyPlan;
        ControlledPlan = updatedEntity.ControlledPlan;
        FreePlan = updatedEntity.FreePlan;
    }
}

public class DailyBillingPlanConfig
{
    public decimal DailyRate { get; private set; }
    public decimal PricePerKilometer { get; private set; }

    [ExcludeFromCodeCoverage]
    private DailyBillingPlanConfig() { }

    public DailyBillingPlanConfig(decimal dailyRate, decimal pricePerKilometer)
    {
        DailyRate = dailyRate;
        PricePerKilometer = pricePerKilometer;
    }
}

public class ControlledBillingPlanConfig
{
    public decimal DailyRate { get; private set; }
    public decimal ExtraPricePerKilometer { get; private set; }

    [ExcludeFromCodeCoverage]
    private ControlledBillingPlanConfig() { }

    public ControlledBillingPlanConfig(decimal dailyRate, decimal extraPricePerKilometer)
    {
        DailyRate = dailyRate;
        ExtraPricePerKilometer = extraPricePerKilometer;
    }
}

public class FreeBillingPlanConfig
{
    public decimal DailyRate { get; private set; }

    [ExcludeFromCodeCoverage]
    private FreeBillingPlanConfig() { }

    public FreeBillingPlanConfig(decimal dailyRate)
    {
        DailyRate = dailyRate;
    }
}