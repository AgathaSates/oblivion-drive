namespace OblivionDrive.Application.DriverModule.DTOs;

public record DetailDriverDTO(
    Guid Id,
    string Name,
    string Email,
    string PhoneNumber,
    string Cpf,
    string Cnh,
    DateOnly CnhExpirationDate,
    Guid ClientId,
    bool IsClientAlsoDriver
);