using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.PartnerModule.Commands;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.PartnerModule.Handlers;
public class DeletePartnerHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryPartner partnerRepository,
    IRepositoryCoupon couponRepository, IValidator<DeletePartnerCommand> validator, IUnitOfWork unitOfWork,
    ILogger<DeletePartnerHandler> logger) : IRequestHandler<DeletePartnerCommand, Result>
{
    public async Task<Result> Handle(DeletePartnerCommand command, CancellationToken cancellationToken)
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
            return Result.Fail(
                ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        Guid currentUserId = tenantProvider.UserId.Value;

        User? currentUser = await userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser is null)
            return Result.Fail(
                ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Partner? partner = await partnerRepository.GetByIdAsync(command.PartnerId);

            if (partner is null)
                return Result.Fail(
                    ErrorResults.RecordNotFoundError(command.PartnerId));

            if (partner.CompanyId != currentCompanyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido excluir parceiros de outra empresa."));

            var allCoupons = await couponRepository.GetAllAsync();
            bool partnerHasCoupons = allCoupons.Any(c => c.PartnerId == partner.Id);

            if (partnerHasCoupons)
                return Result.Fail(ErrorResults.InvalidRequestError("Não é permitido excluir parceiros vinculados a cupons."));


            await partnerRepository.DeleteAsync(partner);
            await unitOfWork.CommitAsync();

            return Result.Ok();
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a exclusão de parceiro {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}