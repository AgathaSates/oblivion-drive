namespace OblivionDrive.Api.Models.CouponModule.Requests;

public record UpdateCouponRequest(
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);