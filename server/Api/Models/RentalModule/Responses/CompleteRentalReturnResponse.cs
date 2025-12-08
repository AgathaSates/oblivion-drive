namespace OblivionDrive.Api.Models.RentalModule.Responses;

public record CompleteRentalReturnResponse(
    bool CompletedSuccessfully,
    Guid RentalId,
    decimal GrossRentalAmount,
    decimal FinalAmountToPay,
    Guid? CouponId,
    decimal CouponDiscountAmount
);