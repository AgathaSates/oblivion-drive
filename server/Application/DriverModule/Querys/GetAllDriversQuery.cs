using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.DriverModule.DTOs;

namespace OblivionDrive.Application.DriverModule.Querys;
public record GetAllDriversQuery(int? Quantity) : IRequest<Result<DriversResult>>;

public record DriversResult(ImmutableList<DetailDriverDTO> Drivers);