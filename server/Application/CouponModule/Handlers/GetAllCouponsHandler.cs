using System.Collections.Immutable;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.CouponModule.DTOs;
using OblivionDrive.Application.CouponModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.CouponModule;

namespace OblivionDrive.Application.CouponModule.Handlers;

public sealed class GetAllCouponsHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryCoupon couponRepository,
    IMapper mapper, ILogger<GetAllCouponsHandler> logger, IValidator<GetAllCouponsQuery> validator)
    : IRequestHandler<GetAllCouponsQuery, Result<CouponsResult>>
{
    public async Task<Result<CouponsResult>> Handle(GetAllCouponsQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(query, cancellationToken);

        if (!validationResult.IsValid)
        {
            List<string> validationErrors = validationResult.Errors
                .Select(error => error.ErrorMessage)
                .ToList();

            return Result.Fail(
                ErrorResults.InvalidRequestError(validationErrors));
        }

        if (tenantProvider.UserId is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        Guid currentUserId = tenantProvider.UserId.Value;

        User? currentUser = await userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            IReadOnlyCollection<Coupon> coupons = query.Quantity.HasValue
                ? await couponRepository.GetAllAsync(query.Quantity.Value)
                : await couponRepository.GetAllAsync();

            List<DetailCouponDTO> couponDtos = mapper.Map<List<DetailCouponDTO>>(coupons);

            CouponsResult result = new(couponDtos.ToImmutableList());

            return Result.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de cupons da empresa {@Query}.",
                query);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}