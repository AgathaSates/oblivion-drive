using System.Collections.Immutable;
using OblivionDrive.Application.CouponModule.DTOs;

namespace OblivionDrive.Api.Models.CouponModule.Responses;

public record GetAllCouponsResponse(int Quantity, ImmutableList<DetailCouponDTO> Coupons);