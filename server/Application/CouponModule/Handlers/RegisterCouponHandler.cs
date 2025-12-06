using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.CouponModule.Commands;
using OblivionDrive.Application.CouponModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.CouponModule.Handlers;
public class RegisterCouponHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryCoupon couponRepository,
    IRepositoryPartner partnerRepository, IUnitOfWork unitOfWork, IValidator<RegisterCouponCommand> validator,
    ILogger<RegisterCouponCommand> logger, IMapper mapper)
    : IRequestHandler<RegisterCouponCommand, Result<CouponDTO>>
{
    public async Task<Result<CouponDTO>> Handle(RegisterCouponCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, cancellationToken);

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

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Partner? partner = await partnerRepository.GetByIdAsync(command.PartnerId);

            if (partner is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.PartnerId));

            if (partner.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido registrar cupons para parceiros de outra empresa."));

            bool couponNameAlreadyExists = await couponRepository.ExistsByNameAsync(command.Name);

            if (couponNameAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cupom cadastrado com este nome para esta empresa."));

            Coupon coupon = new(
                name: command.Name,
                value: command.Value,
                expirationDate: command.ExpirationDate,
                partnerId: command.PartnerId,
                companyId: companyId);

            await couponRepository.AddAsync(coupon);
            await unitOfWork.CommitAsync();

            CouponDTO couponDto = mapper.Map<CouponDTO>(coupon);

            return Result.Ok(couponDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante o registro de cupom {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}
