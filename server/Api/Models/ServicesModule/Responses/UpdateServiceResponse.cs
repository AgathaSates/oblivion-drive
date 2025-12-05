using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Api.Models.ServicesModule.Responses;

public record UpdateServiceResponse(
    bool UpdatedSuccessfully,
    string Name,
    decimal Price,
    ChargeType ChargeType);