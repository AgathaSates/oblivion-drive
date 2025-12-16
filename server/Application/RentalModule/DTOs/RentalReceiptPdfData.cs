namespace OblivionDrive.Application.RentalModule.DTOs;
public record RentalReceiptPdfData(
    Guid RentalId,

    string ClientName,
    string DriverName,

    string VehicleBrand,
    string VehicleModel,
    string VehicleLicensePlate,

    string PlanTypeDisplayName,

    DateOnly StartDate,
    DateOnly ExpectedReturnDate,
    DateOnly ActualReturnDate,

    bool HasDamage,
    bool IsFuelTankFullOnReturn,

    decimal RentalBasePrice,
    decimal InsuranceTotalPrice,
    decimal ServicesTotalPrice,

    decimal FuelChargePrice,
    decimal PenaltyPrice,

    decimal SecurityDepositAmount,
    decimal CouponDiscountAmount,

    decimal EstimatedRentalAmount,
    decimal GrossRentalAmount,
    decimal FinalAmountToPay
);