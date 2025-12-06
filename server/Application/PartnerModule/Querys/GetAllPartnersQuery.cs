using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.PartnerModule.DTOs;

namespace OblivionDrive.Application.PartnerModule.Querys;
public record GetAllPartnersQuery(int? Quantity) : IRequest<Result<PartnersResult>>;

public record PartnersResult(ImmutableList<DetailPartnerDTO> Partners);