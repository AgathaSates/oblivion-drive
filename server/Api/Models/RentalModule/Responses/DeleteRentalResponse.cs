namespace OblivionDrive.Api.Models.RentalModule.Responses;

public record DeleteRentalResponse(bool DeletedSuccessfully, Guid RentalId);