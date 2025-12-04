using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.VehicleGroupModule;
public class VehicleGroupOrmRepository(OblivionDriveDbContext context) : BaseRepository<VehicleGroup>(context), IRepositoryVehicleGroup
{
    public async Task<bool> ExistsByNameAsync(string vehicleGroupName, Guid vehicleGroupIdToIgnore)
    {
        return await context.VehicleGroups.AnyAsync(vg => vg.Name == vehicleGroupName && vg.Id != vehicleGroupIdToIgnore);
    }

    public async Task<bool> ExistsByNameAsync(string vehicleGroupName)
    {
       return await context.VehicleGroups.AnyAsync(vg => vg.Name == vehicleGroupName);
    }
}