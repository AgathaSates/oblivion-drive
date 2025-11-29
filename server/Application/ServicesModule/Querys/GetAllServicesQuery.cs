using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.ServicesModule.DTOs;

namespace OblivionDrive.Application.ServicesModule.Querys;
public record GetAllServicesQuery(int? Quantity) : IRequest<Result<ServicesResult>>;

public record ServicesResult(ImmutableList<DetailServiceDTO> Services);
