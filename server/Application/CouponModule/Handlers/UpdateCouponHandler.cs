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
public class UpdateCouponHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryCoupon couponRepository,
    IRepositoryPartner partnerRepository, IUnitOfWork unitOfWork, IValidator<UpdateCouponCommand> validator,
    ILogger<UpdateCouponCommand> logger, IMapper mapper)
    : IRequestHandler<UpdateCouponCommand, Result<UpdatedCouponDTO>>
{
    public async Task<Result<UpdatedCouponDTO>> Handle(UpdateCouponCommand command, CancellationToken cancellationToken)
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
            Coupon? existingCoupon = await couponRepository.GetByIdAsync(command.CouponId);

            if (existingCoupon is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.CouponId));

            if (existingCoupon.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido editar cupons de outra empresa."));

            Partner? partner = await partnerRepository.GetByIdAsync(command.PartnerId);

            if (partner is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.PartnerId));

            if (partner.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido vincular cupons a parceiros de outra empresa."));

            bool couponNameAlreadyExists = await couponRepository.ExistsByNameAsync(command.Name, command.CouponId);

            if (couponNameAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cupom cadastrado com este nome para esta empresa."));           

            Coupon updatedData = new(
                name: command.Name,
                value: command.Value,
                expirationDate: command.ExpirationDate,
                partnerId: command.PartnerId,
                companyId: existingCoupon.CompanyId);

            Coupon updatedCoupon = await couponRepository.UpdateAsync(existingCoupon, updatedData);

            await unitOfWork.CommitAsync();

            UpdatedCouponDTO dto = mapper.Map<UpdatedCouponDTO>(updatedCoupon);

            return Result.Ok(dto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização do cupom {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}