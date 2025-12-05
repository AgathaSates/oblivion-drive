using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.VehicleModule.DTOs;

namespace OblivionDrive.Application.VehicleModule.Querys;
public record GetAllVehiclesQuery(
    Guid? VehicleGroupId,
    int? Quantity
) : IRequest<Result<VehiclesResult>>;

public record VehiclesResult(ImmutableList<DetailVehicleDTO> Vehicles);