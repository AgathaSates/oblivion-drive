namespace OblivionDrive.Domain.RentalModule;
public record RentalSummaryRow(
    Guid RentalId,
    string ClientName,
    string VehicleBrand,
    string VehicleModel,
    string VehicleLicensePlate,
    RentalPlanType PlanType,
    DateOnly StartDate,
    DateOnly? ActualReturnDate,
    bool IsCompleted,
    decimal GrossRentalAmount,
    decimal FinalAmountToPay,
    decimal CouponDiscountAmount
);