using FluentResults;
using MediatR;
using OblivionDrive.Application.CouponModule.DTOs;

namespace OblivionDrive.Application.CouponModule.Querys;

public record GetCouponByIdQuery(Guid CouponId) : IRequest<Result<DetailCouponDTO>>;