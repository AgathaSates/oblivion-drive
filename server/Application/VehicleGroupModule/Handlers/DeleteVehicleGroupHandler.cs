using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.Shared;
using OblivionDrive.Application.VehicleGroupModule.commands;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Application.VehicleGroupModule.Handlers;
public class DeleteVehicleGroupHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryVehicleGroup vehicleGroupRepository,
    IRepositoryBillingPlan billingPlanRepository, IValidator<DeleteVehicleGroupCommand> validator,
    IUnitOfWork unitOfWork, ILogger<DeleteVehicleGroupHandler> logger) : IRequestHandler<DeleteVehicleGroupCommand, Result>
{
    public async Task<Result> Handle(DeleteVehicleGroupCommand command, CancellationToken cancellationToken)
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
            VehicleGroup? vehicleGroup = await vehicleGroupRepository.GetByIdAsync(command.VehicleGroupId);

            if (vehicleGroup is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.VehicleGroupId));

            if (vehicleGroup.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido excluir grupos de veículos de outra empresa."));

            bool groupUsedByBillingPlans = await billingPlanRepository.ExistsForVehicleGroupAsync(vehicleGroup.Id);

            if (groupUsedByBillingPlans)
                return Result.Fail(ErrorResults.InvalidRequestError(
                    "Não é permitido excluir grupos de veículos que estejam vinculados a planos de cobrança."));           

            await vehicleGroupRepository.DeleteAsync(vehicleGroup);
            await unitOfWork.CommitAsync();

            return Result.Ok();
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a exclusão de grupo de veículos {@Command}.",
                command
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}