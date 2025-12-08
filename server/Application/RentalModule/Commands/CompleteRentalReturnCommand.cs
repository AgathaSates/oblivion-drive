using FluentResults;
using MediatR;
using OblivionDrive.Application.RentalModule.DTOs;

namespace OblivionDrive.Application.RentalModule.Commands;
public record CompleteRentalReturnCommand(
    Guid RentalId,
    DateOnly ActualReturnDate,
    int InitialOdometerInKm,
    int CurrentOdometerInKm,
    bool IsFuelTankFullOnReturn,
    bool HasDamage,
    string? CouponName)
    : IRequest<Result<CompletedRentalDTO>>;