using System.Collections.Immutable;
using OblivionDrive.Application.DriverModule.DTOs;

namespace OblivionDrive.Api.Models.DriverModule.Responses;

public record GetAllDriversResponse(int Quantity, ImmutableList<DetailDriverDTO> Drivers);
