using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.RentalModule.DTOs;

namespace OblivionDrive.Application.RentalModule.Querys;
public record GetAllRentalsQuery(int? Quantity) : IRequest<Result<RentalsResult>>;

public record RentalsResult(ImmutableList<DetailRentalDTO> Rentals);