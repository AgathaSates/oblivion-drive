using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.FuelPriceConfigurationModule.Commands;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.FuelPriceConfigurationModule.Handlers;
public class UpdateFuelPriceConfigurationHandler(
    IValidator<UpdateFuelPriceConfigurationCommand> validator, ITenantProvider tenantProvider,
    UserManager<User> userManager, IRepositoryFuelPriceSettings fuelPriceSettingsRepository,
    IMapper mapper, ILogger<UpdateFuelPriceConfigurationCommand> logger, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateFuelPriceConfigurationCommand, Result<FuelPriceConfigurationDto>>
{
    public async Task<Result<FuelPriceConfigurationDto>> Handle(UpdateFuelPriceConfigurationCommand command, CancellationToken cancellationToken)
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

        if (currentUser.UserType != UserType.Company)
            return Result.Fail(ErrorResults.UnauthorizedError("Apenas usuários do tipo empresa podem configurar os preços de combustível."));
        
        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            var existsConfiguration = await fuelPriceSettingsRepository.GetAsync(companyId);

            var updatedConfiguration = new FuelPriceConfiguration(
                gasoline: command.Gasoline,
                gas: command.Gas,
                diesel: command.Diesel,
                alcohol: command.Alcohol,
                companyId: companyId);

            await fuelPriceSettingsRepository.SaveAsync(updatedConfiguration, companyId);
            await unitOfWork.CommitAsync();

            FuelPriceConfigurationDto configurationDto = mapper.Map<FuelPriceConfigurationDto>(updatedConfiguration);

            return Result.Ok(configurationDto);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro ao atualizar a configuração de preços de combustível {@Command}.",
                command
            );

            return Result.Fail(
                ErrorResults.InternalExceptionError(exception)
            );
        }
    }
}