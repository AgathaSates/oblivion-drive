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
public sealed class GetCouponByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryCoupon couponRepository,
    IMapper mapper, ILogger<GetCouponByIdHandler> logger, IValidator<GetCouponByIdQuery> validator)
    : IRequestHandler<GetCouponByIdQuery, Result<DetailCouponDTO>>
{
    public async Task<Result<DetailCouponDTO>> Handle(GetCouponByIdQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(query, cancellationToken);

        if (!validationResult.IsValid)
        {
            List<string> validationErrors = validationResult.Errors
                .Select(error => error.ErrorMessage)
                .ToList();

            return Result.Fail(ErrorResults.InvalidRequestError(validationErrors));
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
            Coupon? coupon = await couponRepository.GetByIdAsync(query.CouponId);

            if (coupon is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.CouponId));

            if (coupon.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido visualizar cupons de outra empresa."));

            DetailCouponDTO dto = mapper.Map<DetailCouponDTO>(coupon);

            return Result.Ok(dto);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do cupom {@Query}.",
                query);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}