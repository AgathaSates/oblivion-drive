namespace OblivionDrive.Api.Models.VehicleModule.Responses;

public record DeleteVehicleResponse(bool DeletedSuccessfully, Guid VehicleId);