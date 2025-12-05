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
public class UpdateVehicleHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryVehicle vehicleRepository,
    IRepositoryVehicleGroup vehicleGroupRepository, IUnitOfWork unitOfWork, IValidator<UpdateVehicleCommand> validator,
    ILogger<UpdateVehicleCommand> logger, IMapper mapper) : IRequestHandler<UpdateVehicleCommand, Result<UpdatedVehicleDTO>>
{
    public async Task<Result<UpdatedVehicleDTO>> Handle(UpdateVehicleCommand command, CancellationToken cancellationToken)
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
            Vehicle? existingVehicle = await vehicleRepository.GetByIdAsync(command.VehicleId);

            if (existingVehicle is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.VehicleId));

            if (existingVehicle.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido editar veículos de outra empresa."));

            VehicleGroup? vehicleGroup = await vehicleGroupRepository.GetByIdAsync(command.VehicleGroupId);

            if (vehicleGroup is null)
                return Result.Fail(
                    ErrorResults.RecordNotFoundError(command.VehicleGroupId));

            if (vehicleGroup.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido vincular veículos a grupos de outra empresa."));


            Vehicle updatedData = CreateUpdatedVehicle(command, existingVehicle.CompanyId, existingVehicle.PhotoBytes);

            Vehicle updatedVehicle = await vehicleRepository.UpdateAsync(existingVehicle, updatedData);

            await unitOfWork.CommitAsync();

            UpdatedVehicleDTO dto = mapper.Map<UpdatedVehicleDTO>(updatedVehicle);

            return Result.Ok(dto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização do veículo {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private static Vehicle CreateUpdatedVehicle(UpdateVehicleCommand command, Guid companyId, byte[]? existingPhotoBytes)
    {
        Vehicle updatedVehicle = new(
            licensePlate: string.Empty,
            brand: command.Brand,
            model: command.Model,
            color: command.Color,
            fuelType: command.FuelType,
            fuelTankCapacityInLiters: command.FuelTankCapacityInLiters,
            year: command.Year,
            vehicleGroupId: command.VehicleGroupId,
            companyId: companyId);

        if (command.PhotoBytes is not null && command.PhotoBytes.Length > 0)
        {
            updatedVehicle.SetPhoto(command.PhotoBytes);
        }
        else if (existingPhotoBytes is not null && existingPhotoBytes.Length > 0)
        {
            updatedVehicle.SetPhoto(existingPhotoBytes);
        }

        return updatedVehicle;
    }
}