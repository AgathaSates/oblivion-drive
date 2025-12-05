namespace OblivionDrive.Api.Models.DriverModule.Responses;

public record RegisterDriverResponse(
    bool CreatedSuccessfully,
    string Name,
    string Email,
    string PhoneNumber,
    string Cpf,
    string Cnh,
    DateOnly CnhExpirationDate,
    Guid ClientId,
    bool IsClientAlsoDriver
);