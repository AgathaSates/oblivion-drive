using System.Collections.Immutable;
using OblivionDrive.Application.VehicleGroupModule.DTOs;

namespace OblivionDrive.Api.Models.VehicleGroupModule;

public record GetAllVehicleGroupResponse(int Quantity, ImmutableList<DetailVehicleGroupDTO> VehicleGroups);