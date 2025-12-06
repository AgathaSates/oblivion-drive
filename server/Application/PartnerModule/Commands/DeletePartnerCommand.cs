using FluentResults;
using MediatR;

namespace OblivionDrive.Application.PartnerModule.Commands;
public record DeletePartnerCommand(Guid PartnerId) : IRequest<Result>;
