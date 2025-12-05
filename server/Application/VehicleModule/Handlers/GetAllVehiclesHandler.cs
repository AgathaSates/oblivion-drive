using System.Collections.Immutable;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.Shared;
using OblivionDrive.Application.VehicleModule.DTOs;
using OblivionDrive.Application.VehicleModule.Querys;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Application.VehicleModule.Handlers;
public class GetAllVehiclesHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryVehicle vehicleRepository,
    IMapper mapper, ILogger<GetAllVehiclesHandler> logger, IValidator<GetAllVehiclesQuery> validator)
    : IRequestHandler<GetAllVehiclesQuery, Result<VehiclesResult>>
{
    public async Task<Result<VehiclesResult>> Handle(GetAllVehiclesQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(query, cancellationToken);

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
            IReadOnlyCollection<Vehicle> vehicles;

            if (query.VehicleGroupId.HasValue)
            {
                List<Vehicle> vehiclesByGroup = await vehicleRepository.GetByVehicleGroupAsync(query.VehicleGroupId.Value);

                vehicles = vehiclesByGroup;
            }
            else
            {
                vehicles = query.Quantity.HasValue
                    ? await vehicleRepository.GetAllAsync(query.Quantity.Value)
                    : await vehicleRepository.GetAllAsync();
            }

            List<DetailVehicleDTO> vehicleDtos = mapper.Map<List<DetailVehicleDTO>>(vehicles);

            VehiclesResult result = new(vehicleDtos.ToImmutableList());

            return Result.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de veículos da empresa {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}