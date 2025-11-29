using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Api.Models.ServicesModule;

public record RegisterServiceRequest(string Name, decimal Price, ChargeType chargetype);