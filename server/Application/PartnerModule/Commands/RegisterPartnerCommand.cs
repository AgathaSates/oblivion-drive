using FluentResults;
using MediatR;
using OblivionDrive.Application.PartnerModule.DTOs;

namespace OblivionDrive.Application.PartnerModule.Commands;

public record RegisterPartnerCommand(string Name) : IRequest<Result<PartnerDTO>>;