using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Application.ServicesModule.DTOs;

public record UpdatedServiceDTO(
    bool UpdatedSuccessfully,
    string Name,
    decimal Price,
    ChargeType ChargeType);