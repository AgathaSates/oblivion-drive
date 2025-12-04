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
public class GetVehicleGroupByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryVehicleGroup vehicleGroupRepository,
    IMapper mapper, ILogger<GetVehicleGroupByIdHandler> logger, IValidator<GetVehicleGroupByIdQuery> validator)
    : IRequestHandler<GetVehicleGroupByIdQuery, Result<DetailVehicleGroupDTO>>
{
    public async Task<Result<DetailVehicleGroupDTO>> Handle(GetVehicleGroupByIdQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(query, cancellationToken);

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
            VehicleGroup? vehicleGroup = await vehicleGroupRepository.GetByIdAsync(query.VehicleGroupId);

            if (vehicleGroup is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.VehicleGroupId));

            if (vehicleGroup.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido visualizar grupos de veículos de outra empresa."));

            DetailVehicleGroupDTO vehicleGroupDetail = mapper.Map<DetailVehicleGroupDTO>(vehicleGroup);

            return Result.Ok(vehicleGroupDetail);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do grupo de veículos {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}