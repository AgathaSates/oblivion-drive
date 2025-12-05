using FluentResults;
using MediatR;
using OblivionDrive.Application.ClientModule.DTOs;

namespace OblivionDrive.Application.ClientModule.Querys;
public record GetClientByIdQuery(Guid ClientId) : IRequest<Result<DetailClientDTO>>;