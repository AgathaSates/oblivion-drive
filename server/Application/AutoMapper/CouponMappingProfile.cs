using AutoMapper;
using OblivionDrive.Application.CouponModule.DTOs;
using OblivionDrive.Domain.CouponModule;

namespace OblivionDrive.Application.AutoMapper;
public class CouponMappingProfile : Profile
{
    public CouponMappingProfile()
    {
        CreateMap<Coupon, CouponDTO>()
            .ConstructUsing(coupon => new CouponDTO(
                true,
                coupon.Name,
                coupon.Value,
                coupon.ExpirationDate,
                coupon.PartnerId
            ));

        CreateMap<Coupon, UpdatedCouponDTO>()
            .ConstructUsing(coupon => new UpdatedCouponDTO(
                true,
                coupon.Name,
                coupon.Value,
                coupon.ExpirationDate,
                coupon.PartnerId
            ));

        CreateMap<Coupon, DetailCouponDTO>()
            .ConstructUsing(coupon => new DetailCouponDTO(
                coupon.Id,
                coupon.Name,
                coupon.Value,
                coupon.ExpirationDate,
                coupon.PartnerId
            ));
    }
}