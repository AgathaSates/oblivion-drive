namespace OblivionDrive.Application.RentalModule.DTOs;

public record CompletedRentalDTO(
    bool CompletedSuccessfully,
    Guid RentalId,
    decimal GrossRentalAmount,
    decimal FinalAmountToPay,
    Guid? CouponId,
    decimal CouponDiscountAmount);