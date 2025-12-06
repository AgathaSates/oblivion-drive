namespace OblivionDrive.Api.Models.CouponModule.Responses;

public record RegisterCouponResponse(
    bool CreatedSuccessfully,
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);