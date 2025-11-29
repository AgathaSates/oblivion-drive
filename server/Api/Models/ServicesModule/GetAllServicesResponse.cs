using System.Collections.Immutable;
using OblivionDrive.Application.ServicesModule.DTOs;

namespace OblivionDrive.Api.Models.ServicesModule;

public record GetAllServicesResponse(int Quantity, ImmutableList<DetailServiceDTO> Services);