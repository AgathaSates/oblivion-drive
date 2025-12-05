namespace OblivionDrive.Application.DriverModule.DTOs;
public record DriverDTO(
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
