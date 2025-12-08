using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.BillingPlanModule;
public class BillingPlanOrmRepository(OblivionDriveDbContext context) : BaseRepository<BillingPlan>(context), IRepositoryBillingPlan
{
    public async Task<bool> ExistsByNameAsync(string vehicleGroupName)
    {
       return await context.BillingPlans.AnyAsync(plan => plan.Name == vehicleGroupName);
    }

    public async Task<bool> ExistsByNameAsync(string vehicleGroupName, Guid vehicleGroupIdToIgnore)
    {
        return await context.BillingPlans.AnyAsync(plan => plan.Name == vehicleGroupName && plan.Id != vehicleGroupIdToIgnore);
    }

    public async Task<bool> ExistsForVehicleGroupAsync(Guid vehicleGroupId, Guid billingPlanIdToIgnore)
    {
        return await context.BillingPlans
        .AnyAsync(billingPlan =>
            billingPlan.VehicleGroupId == vehicleGroupId && billingPlan.Id != billingPlanIdToIgnore);
    }

    public async Task<bool> ExistsForVehicleGroupAsync(Guid vehicleGroupId)
    {
        return await context.BillingPlans
        .AnyAsync(billingPlan => billingPlan.VehicleGroupId == vehicleGroupId);
    }

    public async Task<BillingPlan?> GetByVehicleGroupIdAsync(Guid vehicleGroupId)
    {
        return await context.BillingPlans
            .FirstOrDefaultAsync(plan => plan.VehicleGroupId == vehicleGroupId);
    }
}
