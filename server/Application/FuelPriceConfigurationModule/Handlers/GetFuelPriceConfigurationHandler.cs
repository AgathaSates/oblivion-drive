using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.FuelPriceConfigurationModule.DTOs;
using OblivionDrive.Application.FuelPriceConfigurationModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Application.FuelPriceConfigurationModule.Handlers;
public class GetFuelPriceConfigurationHandler(
    ITenantProvider tenantProvider, UserManager<User> userManager,
    IRepositoryFuelPriceSettings fuelPriceSettingsRepository,
    IMapper mapper, ILogger<GetFuelPriceConfigurationQuery> logger)
    : IRequestHandler<GetFuelPriceConfigurationQuery, Result<FuelPriceConfigurationDto>>
{
    public async Task<Result<FuelPriceConfigurationDto>> Handle(GetFuelPriceConfigurationQuery query, CancellationToken cancellationToken)
    {
        if (tenantProvider.UserId is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        Guid currentUserId = tenantProvider.UserId.Value;

        User? currentUser = await userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            FuelPriceConfiguration configuration = await fuelPriceSettingsRepository.GetAsync(companyId);

            FuelPriceConfigurationDto configurationDto = mapper.Map<FuelPriceConfigurationDto>(configuration);

            return Result.Ok(configurationDto);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro ao obter a configuração de preços de combustível {@Query}.",
                query
            );

            return Result.Fail(
                ErrorResults.InternalExceptionError(exception)
            );
        }
    }
}