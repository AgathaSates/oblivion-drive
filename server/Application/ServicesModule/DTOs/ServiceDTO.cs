using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Application.ServicesModule.DTOs;

public record ServiceDTO(
    bool CreatedSuccessfully,
    string Name,
    decimal Price,
    ChargeType ChargeType);