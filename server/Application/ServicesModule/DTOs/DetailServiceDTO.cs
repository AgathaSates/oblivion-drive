using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Application.ServicesModule.DTOs;
public record DetailServiceDTO(
    Guid Id,
    string Name, 
    decimal Price,
    ChargeType ChargeType);