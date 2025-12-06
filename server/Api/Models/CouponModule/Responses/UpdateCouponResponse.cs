namespace OblivionDrive.Api.Models.CouponModule.Responses;

public record UpdateCouponResponse(
    bool UpdatedSuccessfully,
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);