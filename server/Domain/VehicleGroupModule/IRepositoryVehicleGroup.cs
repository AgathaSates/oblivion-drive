using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.VehicleGroupModule;
public interface IRepositoryVehicleGroup : IRepository<VehicleGroup>
{
    Task<bool> ExistsByNameAsync(string vehicleGroupName, Guid vehicleGroupIdToIgnore);
    Task<bool> ExistsByNameAsync(string vehicleGroupName);
}