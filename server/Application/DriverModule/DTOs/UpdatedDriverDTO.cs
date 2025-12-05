namespace OblivionDrive.Application.DriverModule.DTOs;
public record UpdatedDriverDTO(
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