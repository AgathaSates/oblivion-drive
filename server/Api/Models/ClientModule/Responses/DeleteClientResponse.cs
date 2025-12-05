namespace OblivionDrive.Api.Models.ClientModule.Responses;

public record DeleteClientResponse(bool DeletedSuccessfully, Guid ClientId);