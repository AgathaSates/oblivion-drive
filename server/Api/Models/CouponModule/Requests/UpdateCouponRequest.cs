namespace OblivionDrive.Api.Models.CouponModule.Requests;

public record UpdateCouponRequest(
    Guid CouponId,
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);