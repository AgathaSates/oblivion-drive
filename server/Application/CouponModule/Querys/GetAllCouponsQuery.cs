using System.Collections.Immutable;
using FluentResults;
using MediatR;
using OblivionDrive.Application.CouponModule.DTOs;

namespace OblivionDrive.Application.CouponModule.Querys;
public record GetAllCouponsQuery(int? Quantity) : IRequest<Result<CouponsResult>>;

public record CouponsResult(ImmutableList<DetailCouponDTO> Coupons);
