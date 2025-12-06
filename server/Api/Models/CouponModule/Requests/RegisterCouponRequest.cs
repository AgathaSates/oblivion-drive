namespace OblivionDrive.Api.Models.CouponModule.Requests;

public record RegisterCouponRequest(
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);