using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Api.Models.ServicesModule;

public record UpdateServiceRequest(string Name, decimal Price, ChargeType ChargeType);