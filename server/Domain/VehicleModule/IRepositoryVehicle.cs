using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Domain.VehicleModule;
public interface IRepositoryVehicle : IRepository<Vehicle>
{
    Task AddPhotoAsync(Guid vehicleId, byte[] photoByte);
    Task<List<Vehicle>> GetByVehicleGroupAsync(Guid vehicleGroupId);
}