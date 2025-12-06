using System.Collections.Immutable;
using AutoMapper;
using OblivionDrive.Api.Models.CouponModule.Requests;
using OblivionDrive.Api.Models.CouponModule.Responses;
using OblivionDrive.Application.CouponModule.Commands;
using OblivionDrive.Application.CouponModule.DTOs;
using OblivionDrive.Application.CouponModule.Querys;

namespace OblivionDrive.Api.AutoMapper;

public sealed class CouponsModelsMappingProfile : Profile
{
    public CouponsModelsMappingProfile()
    {
        CreateMap<RegisterCouponRequest, RegisterCouponCommand>();
        CreateMap<(Guid, UpdateCouponRequest), UpdateCouponCommand>()
            .ConvertUsing(src => new UpdateCouponCommand(
                src.Item1,
                src.Item2.Name,
                src.Item2.Value,
                src.Item2.ExpirationDate,
                src.Item2.PartnerId
            ));

        CreateMap<CouponDTO, RegisterCouponResponse>();
        CreateMap<UpdatedCouponDTO, UpdateCouponResponse>();
        CreateMap<DetailCouponDTO, GetCouponByIdResponse>();
        CreateMap<CouponsResult, GetAllCouponsResponse>()
            .ConvertUsing((src, dest, ctx) => new GetAllCouponsResponse(
                Quantity: src.Coupons.Count,
                Coupons: src?.Coupons?
                    .Select(c => ctx.Mapper.Map<DetailCouponDTO>(c))
                    .ToImmutableList() ?? ImmutableList<DetailCouponDTO>.Empty
            ));
    }
}