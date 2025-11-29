using FluentResults;
using MediatR;
using OblivionDrive.Application.ServicesModule.DTOs;

namespace OblivionDrive.Application.ServicesModule.Querys;
public record GetServiceByIdQuery(Guid ServiceId) : IRequest<Result<DetailServiceDTO>>;