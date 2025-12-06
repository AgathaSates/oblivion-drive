namespace OblivionDrive.Application.CouponModule.DTOs;
public record CouponDTO(
    bool CreatedSuccessfully,
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
);