using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.RentalModule;

public interface IRepositoryRental : IRepository<Rental> 
{
    Task<bool> ExistsOpenRentalForVehicleAsync(Guid vehicleId);
    Task<bool> ExistsOpenRentalForVehicleAsync(Guid vehicleId, Guid rentalIdToIgnore);
    Task<bool> ExistsForVehicleGroupAsync(Guid vehicleGroupId);
    Task<bool> ExistsOpenRentalForClientAsync(Guid clientId);
    Task<bool> ExistsOpenRentalForDriverAsync(Guid driverId);
    Task<bool> ExistsOpenRentalUsingServiceAsync(Guid serviceId);
}