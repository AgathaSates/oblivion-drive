namespace OblivionDrive.Api.Models.DriverModule.Responses;

public record UpdateDriverResponse(
    bool UpdatedSuccessfully,
    string Name,
    string Email,
    string PhoneNumber,
    string Cpf,
    string Cnh,
    DateOnly CnhExpirationDate,
    Guid ClientId,
    bool IsClientAlsoDriver
);