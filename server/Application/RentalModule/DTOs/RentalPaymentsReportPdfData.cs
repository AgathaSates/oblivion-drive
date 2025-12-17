namespace OblivionDrive.Application.RentalModule.DTOs;

public record RentalPaymentsReportPdfData(
    DateTime GeneratedAt,
    IReadOnlyList<RentalPaymentsReportRow> Rows,
    decimal TotalGrossAmount,
    decimal TotalPaidOnReturnAmount,
    decimal TotalCouponDiscountAmount,
    decimal TotalNetAmountAfterCoupons
);

public record RentalPaymentsReportRow(
    Guid RentalId,
    string ClientName,
    string VehicleLabel,
    string PlanTypeLabel,
    DateOnly StartDate,
    DateOnly? ActualReturnDate,
    decimal GrossRentalAmount,
    decimal FinalAmountToPay,
    decimal CouponDiscountAmount
);