using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.BillingPlanModule.Commands;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.BillingPlanModule.Handlers;

public class DeleteBillingPlanHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryBillingPlan billingPlanRepository,
    IRepositoryRental rentalRepository, IValidator<DeleteBillingPlanCommand> validator, IUnitOfWork unitOfWork,
    ILogger<DeleteBillingPlanHandler> logger) : IRequestHandler<DeleteBillingPlanCommand, Result>
{
    public async Task<Result> Handle(DeleteBillingPlanCommand command, CancellationToken cancellationToken)
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

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            BillingPlan? billingPlan = await billingPlanRepository.GetByIdAsync(command.BillingPlanId);

            if (billingPlan is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.BillingPlanId));

            if (billingPlan.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido excluir planos de cobrança de outra empresa."));

            bool billingPlanUsedInRentals = await rentalRepository.ExistsForVehicleGroupAsync(billingPlan.VehicleGroupId);

            if (billingPlanUsedInRentals)
                return Result.Fail(
                    ErrorResults.InvalidRequestError(
                        "Não é permitido excluir planos de cobrança que já foram utilizados em aluguéis."));

            await billingPlanRepository.DeleteAsync(billingPlan);
            await unitOfWork.CommitAsync();

            return Result.Ok();
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a exclusão de plano de cobrança {@Command}.",
                command
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}