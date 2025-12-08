using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.RentalModule;

public interface IRepositoryRental : IRepository<Rental> 
{
    Task<bool> ExistsOpenRentalForVehicleAsync(Guid vehicleId);
    Task<bool> ExistsOpenRentalForVehicleAsync(Guid vehicleId, Guid rentalIdToIgnore);
}