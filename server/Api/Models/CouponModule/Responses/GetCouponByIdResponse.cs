namespace OblivionDrive.Api.Models.CouponModule.Responses;

public record GetCouponByIdResponse(
    Guid Id,
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);