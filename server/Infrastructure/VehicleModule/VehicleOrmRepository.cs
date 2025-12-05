using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.VehicleModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.VehicleModule;
public class VehicleOrmRepository(OblivionDriveDbContext context) : BaseRepository<Vehicle>(context), IRepositoryVehicle
{
    public async Task AddPhotoAsync(Guid vehicleId, byte[] photoByte)
    {
        var vehicle = await context.Vehicles.SingleOrDefaultAsync(v => v.Id == vehicleId);
        vehicle!.SetPhoto(photoByte);
    }

    public async Task<List<Vehicle>> GetByVehicleGroupAsync(Guid vehicleGroupId)
    {
        return await context.Vehicles.Where
            (vehicle => vehicle.VehicleGroupId == vehicleGroupId).ToListAsync();
    }
}