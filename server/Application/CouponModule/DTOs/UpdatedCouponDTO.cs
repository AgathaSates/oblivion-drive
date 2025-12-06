namespace OblivionDrive.Application.CouponModule.DTOs;
public record UpdatedCouponDTO(
    bool UpdatedSuccessfully,
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);