using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.BillingPlanModule.Commands;
using OblivionDrive.Application.BillingPlanModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.BillingPlanModule.Handlers;
public class RegisterBillingPlanHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider,
    IRepositoryBillingPlan billingPlanRepository, IUnitOfWork unitOfWork, IValidator<RegisterBillingPlanCommand> validator,
    ILogger<RegisterBillingPlanCommand> logger,IMapper mapper) : IRequestHandler<RegisterBillingPlanCommand, Result<BillingPlanDTO>>
{
    public async Task<Result<BillingPlanDTO>> Handle(RegisterBillingPlanCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, cancellationToken);

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
        

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            string formattedName = NameFormatter.FormatName(command.Name);

            bool billingPlanNameAlreadyExists = await billingPlanRepository.ExistsByNameAsync(formattedName);

            if (billingPlanNameAlreadyExists)
                return Result.Fail(
                    ErrorResults.InvalidRequestError("Já existe um plano de cobrança cadastrado com este nome para esta empresa."));
            

            bool billingPlanForVehicleGroupAlreadyExists = await billingPlanRepository.ExistsForVehicleGroupAsync(command.VehicleGroupId);

            if (billingPlanForVehicleGroupAlreadyExists)
                return Result.Fail(
                    ErrorResults.InvalidRequestError("Já existe um plano de cobrança cadastrado para este grupo de veículos."));
            

            BillingPlan billingPlan = CreateBillingPlan(command, companyId);

            await billingPlanRepository.AddAsync(billingPlan);
            await unitOfWork.CommitAsync();

            BillingPlanDTO billingPlanDto = mapper.Map<BillingPlanDTO>(billingPlan);

            return Result.Ok(billingPlanDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante o registro de plano de cobrança {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private static BillingPlan CreateBillingPlan(RegisterBillingPlanCommand command, Guid companyId)
    {
        string formattedName = NameFormatter.FormatName(command.Name);

        var dailyPlan = new DailyBillingPlanConfig(
            command.DailyPlanDailyRate,
            command.DailyPlanPricePerKilometer);

        var controlledPlan = new ControlledBillingPlanConfig(
            command.ControlledPlanDailyRate,
            command.ControlledPlanExtraPricePerKilometer);

        var freePlan = new FreeBillingPlanConfig(
            command.FreePlanDailyRate);

        return new BillingPlan(
            formattedName,
            companyId,
            command.VehicleGroupId,
            dailyPlan,
            controlledPlan,
            freePlan);
    }
} 