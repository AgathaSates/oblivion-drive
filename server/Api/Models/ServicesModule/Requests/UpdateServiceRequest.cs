using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Api.Models.ServicesModule.Requests;

public record UpdateServiceRequest(string Name, decimal Price, ChargeType ChargeType);