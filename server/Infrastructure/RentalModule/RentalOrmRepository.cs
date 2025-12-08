using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.RentalModule;

public class RentalOrmRepository(OblivionDriveDbContext context) : BaseRepository<Rental>(context), IRepositoryRental
{
    public async Task<bool> ExistsOpenRentalForVehicleAsync(Guid vehicleId)
    {
        return await context.Rentals
            .AnyAsync(rental =>
                rental.VehicleId == vehicleId &&
                !rental.IsCompleted);
    }

    public async Task<bool> ExistsOpenRentalForVehicleAsync(Guid vehicleId, Guid rentalIdToIgnore)
    {
        return await context.Rentals
            .AnyAsync(rental =>
                rental.VehicleId == vehicleId &&
                !rental.IsCompleted &&
                rental.Id != rentalIdToIgnore);
    }

    public async Task<bool> ExistsForVehicleGroupAsync(Guid vehicleGroupId)
    {
        return await context.Rentals
            .Include(rental => rental.Vehicle)
            .AnyAsync(rental => rental.Vehicle.VehicleGroupId == vehicleGroupId);
    }

    public async Task<bool> ExistsOpenRentalForClientAsync(Guid clientId)
    {
        return await context.Rentals
             .AnyAsync(rental => rental.ClientId == clientId && !rental.IsCompleted);
    }

    public async Task<bool> ExistsOpenRentalForDriverAsync(Guid driverId)
    {
        return await context.Rentals
            .AnyAsync(rental => rental.DriverId == driverId && !rental.IsCompleted);
    }
    public async Task<bool> ExistsOpenRentalUsingServiceAsync(Guid serviceId)
    {
        return await context.Rentals
            .Where(rental => !rental.IsCompleted)
            .AnyAsync(rental => rental.ServiceIds.Contains(serviceId));
    }
}