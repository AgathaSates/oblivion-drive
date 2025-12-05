using System.Collections.Immutable;
using OblivionDrive.Application.VehicleModule.DTOs;

namespace OblivionDrive.Api.Models.VehicleModule.Responses;

public record GetAllVehiclesResponse(int Quantity, ImmutableList<DetailVehicleDTO> Vehicles);