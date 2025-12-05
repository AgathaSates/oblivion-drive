using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Api.Models.ServicesModule;

public record GetServiceByIdResponse(
    Guid Id,
    string Name,
    decimal Price,
    ChargeType ChargeType);
