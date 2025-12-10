namespace OblivionDrive.Api.Models.DriverModule.Requests;

public record UpdateDriverRequest(
    string Name,
    string Email,
    string PhoneNumber,
    string Cpf,
    string Cnh,
    DateOnly CnhExpirationDate,
    Guid ClientId,
    bool IsClientAlsoDriver
);