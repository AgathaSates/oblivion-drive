namespace OblivionDrive.Application.CouponModule.DTOs;
public record DetailCouponDTO(
    Guid Id,
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);