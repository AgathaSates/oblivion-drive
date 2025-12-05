using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.ClientModule.DTOs;

namespace OblivionDrive.Application.ClientModule.Querys;
public record GetAllClientsQuery(int? Quantity) : IRequest<Result<ClientsResult>>;

public record ClientsResult(ImmutableList<DetailClientDTO> Clients);