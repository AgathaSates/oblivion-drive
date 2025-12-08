using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.BillingPlanModule;
public interface IRepositoryBillingPlan : IRepository<BillingPlan>
{
    Task<bool> ExistsForVehicleGroupAsync(Guid vehicleGroupId, Guid billingPlanIdToIgnore);
    Task<bool> ExistsForVehicleGroupAsync(Guid vehicleGroupId);
    Task<bool> ExistsByNameAsync(string vehicleGroupName);
    Task<bool> ExistsByNameAsync(string vehicleGroupName, Guid vehicleGroupIdToIgnore);
    Task<BillingPlan?> GetByVehicleGroupIdAsync(Guid vehicleGroupId);
}