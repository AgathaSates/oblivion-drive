using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Api.Models.ServicesModule.Requests;

public record RegisterServiceRequest(string Name, decimal Price, ChargeType chargetype);