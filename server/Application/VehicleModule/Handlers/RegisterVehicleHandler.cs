using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.Shared;
using OblivionDrive.Application.VehicleModule.Commands;
using OblivionDrive.Application.VehicleModule.DTOs;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Application.VehicleModule.Handlers;
public class RegisterVehicleHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryVehicle vehicleRepository,
    IRepositoryVehicleGroup vehicleGroupRepository, IUnitOfWork unitOfWork, IValidator<RegisterVehicleCommand> validator,
    ILogger<RegisterVehicleCommand> logger, IMapper mapper) : IRequestHandler<RegisterVehicleCommand, Result<VehicleDTO>>
{
    public async Task<Result<VehicleDTO>> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken)
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
            VehicleGroup? vehicleGroup = await vehicleGroupRepository.GetByIdAsync(command.VehicleGroupId);

            if (vehicleGroup is null)
                return Result.Fail(
                    ErrorResults.RecordNotFoundError(command.VehicleGroupId));

            if (vehicleGroup.CompanyId != companyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido registrar veículos em grupos de outra empresa."));

            Vehicle vehicle = CreateVehicle(command, companyId);

            vehicle.SetPhoto(command.PhotoBytes);

            await vehicleRepository.AddAsync(vehicle);
            await unitOfWork.CommitAsync();

            VehicleDTO vehicleDto = mapper.Map<VehicleDTO>(vehicle);

            return Result.Ok(vehicleDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante o registro de veículo {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private static Vehicle CreateVehicle(RegisterVehicleCommand command, Guid companyId)
    {
        return new Vehicle(
            licensePlate: command.LicensePlate,
            brand: command.Brand,
            model: command.Model,
            color: command.Color,
            fuelType: command.FuelType,
            fuelTankCapacityInLiters: command.FuelTankCapacityInLiters,
            year: command.Year,
            vehicleGroupId: command.VehicleGroupId,
            companyId: companyId);
    }
}

