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
public class GetVehicleByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryVehicle vehicleRepository,
    IMapper mapper, ILogger<GetVehicleByIdHandler> logger, IValidator<GetVehicleByIdQuery> validator)
    : IRequestHandler<GetVehicleByIdQuery, Result<DetailVehicleDTO>>
{
    public async Task<Result<DetailVehicleDTO>> Handle(GetVehicleByIdQuery query, CancellationToken cancellationToken)
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
            Vehicle? vehicle = await vehicleRepository.GetByIdAsync(query.VehicleId);

            if (vehicle is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.VehicleId));

            if (vehicle.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido visualizar veículos de outra empresa."));

            DetailVehicleDTO vehicleDetail = mapper.Map<DetailVehicleDTO>(vehicle);

            return Result.Ok(vehicleDetail);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do veículo {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}