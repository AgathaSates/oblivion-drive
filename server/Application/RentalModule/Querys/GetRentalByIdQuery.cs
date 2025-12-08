using FluentResults;
using MediatR;
using OblivionDrive.Application.RentalModule.DTOs;

namespace OblivionDrive.Application.RentalModule.Querys;
public record GetRentalByIdQuery(Guid RentalId) : IRequest<Result<DetailRentalDTO>>;