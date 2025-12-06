using FluentResults;
using MediatR;

namespace OblivionDrive.Application.CouponModule.Commands;
public record DeleteCouponCommand(Guid CouponId) : IRequest<Result>;