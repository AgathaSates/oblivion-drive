namespace OblivionDrive.Application.RentalModule.DTOs;

public record RentalPaymentsReportPdfData(
    DateTime GeneratedAt,
    IReadOnlyList<RentalPaymentsReportRow> Rows,
    decimal TotalGrossAmount,
    decimal TotalFinalAmountToPay
);

public record RentalPaymentsReportRow(
    Guid RentalId,
    string ClientName,
    string VehicleLabel,
    string PlanTypeLabel,
    DateOnly StartDate,
    DateOnly? ActualReturnDate,
    decimal GrossRentalAmount,
    decimal FinalAmountToPay
);