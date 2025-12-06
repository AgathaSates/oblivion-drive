using FluentResults;
using MediatR;
using OblivionDrive.Application.CouponModule.DTOs;

namespace OblivionDrive.Application.CouponModule.Commands;
public record RegisterCouponCommand(
    string Name,
    decimal Value,
    DateOnly ExpirationDate,
    Guid PartnerId
) : IRequest<Result<CouponDTO>>;
