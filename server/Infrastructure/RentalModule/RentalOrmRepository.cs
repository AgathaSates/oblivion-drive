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
}