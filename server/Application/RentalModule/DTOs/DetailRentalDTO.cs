using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Application.RentalModule.DTOs;

public record DetailRentalDTO(
    Guid Id,
    Guid ClientId,
    Guid DriverId,
    Guid VehicleId,
    RentalPlanType PlanType,
    DateOnly StartDate,
    DateOnly ExpectedReturnDate,
    DateOnly? ActualReturnDate,
    decimal EstimatedRentalAmount,
    decimal GrossRentalAmount,
    decimal FinalAmountToPay,
    bool IsCompleted,
    Guid? CouponId,
    IReadOnlyCollection<Guid> ServiceIds);