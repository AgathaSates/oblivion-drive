using FluentResults;
using MediatR;
using OblivionDrive.Application.PartnerModule.DTOs;

namespace OblivionDrive.Application.PartnerModule.Querys;
public record GetPartnerByIdQuery(Guid PartnerId) : IRequest<Result<DetailPartnerDTO>>;