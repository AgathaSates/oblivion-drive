namespace OblivionDrive.Api.Models.RentalModule.Requests;

public record CompleteRentalReturnRequest(
    DateOnly ActualReturnDate,
    int InitialOdometerInKm,
    int CurrentOdometerInKm,
    bool IsFuelTankFullOnReturn,
    bool HasDamage,
    string? CouponName
);