namespace OblivionDrive.Api.Models.DriverModule.Requests;
public record RegisterDriverRequest(
    string Name,
    string Email,
    string PhoneNumber,
    string Cpf,
    string Cnh,
    DateOnly CnhExpirationDate,
    Guid ClientId,
    bool IsClientAlsoDriver
);