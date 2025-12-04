using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.VehicleGroupModule.DTOs;

namespace OblivionDrive.Application.VehicleGroupModule.Querys;
public record GetAllVehicleGroupQuery(int? Quantity) : IRequest<Result<VehicleGroupResult>>;

public record VehicleGroupResult(ImmutableList<DetailVehicleGroupDTO> VehicleGroups);