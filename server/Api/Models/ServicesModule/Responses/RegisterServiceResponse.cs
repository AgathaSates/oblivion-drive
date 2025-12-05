using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Api.Models.ServicesModule;

public record RegisterServiceResponse(
    bool CreatedSuccessfully,
    string Name,
    decimal Price,
    ChargeType ChargeType);