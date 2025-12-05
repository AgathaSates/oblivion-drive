namespace OblivionDrive.Api.Models.DriverModule.Responses;

public record GetDriverByIdResponse(
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