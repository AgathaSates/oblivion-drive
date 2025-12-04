using System.Collections.Immutable;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.Shared;
using OblivionDrive.Application.VehicleGroupModule.DTOs;
using OblivionDrive.Application.VehicleGroupModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Application.VehicleGroupModule.Handlers;
public  class GetAllVehicleGroupHandler(
    UserManager<User> userManager,
    ITenantProvider tenantProvider,
    IRepositoryVehicleGroup vehicleGroupRepository,
    IMapper mapper,
    ILogger<GetAllVehicleGroupHandler> logger,
    IValidator<GetAllVehicleGroupQuery> validator)
    : IRequestHandler<GetAllVehicleGroupQuery, Result<VehicleGroupResult>>
{
    public async Task<Result<VehicleGroupResult>> Handle(GetAllVehicleGroupQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validationResult =
            await validator.ValidateAsync(query, cancellationToken);

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
            IReadOnlyCollection<VehicleGroup> vehicleGroups = query.Quantity.HasValue
                ? await vehicleGroupRepository.GetAllAsync(query.Quantity.Value)
                : await vehicleGroupRepository.GetAllAsync();

            List<DetailVehicleGroupDTO> detailVehicleGroups = mapper.Map<List<DetailVehicleGroupDTO>>(vehicleGroups);

            VehicleGroupResult result = new(detailVehicleGroups.ToImmutableList());

            return Result.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de grupos de veículos da empresa {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}