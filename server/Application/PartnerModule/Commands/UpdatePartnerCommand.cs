using FluentResults;
using MediatR;
using OblivionDrive.Application.PartnerModule.DTOs;

namespace OblivionDrive.Application.PartnerModule.Commands;
public record UpdatePartnerCommand(Guid PartnerId, string Name) : IRequest<Result<UpdatedPartnerDTO>>;