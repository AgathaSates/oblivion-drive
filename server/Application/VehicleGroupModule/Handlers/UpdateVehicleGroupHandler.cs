using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.Shared;
using OblivionDrive.Application.VehicleGroupModule.commands;
using OblivionDrive.Application.VehicleGroupModule.DTOs;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;

namespace OblivionDrive.Application.VehicleGroupModule.Handlers;
public class UpdateVehicleGroupHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryVehicleGroup vehicleGroupRepository,
    IUnitOfWork unitOfWork,IValidator<UpdateVehicleGroupCommand> validator, ILogger<UpdateVehicleGroupCommand> logger, IMapper mapper)
    : IRequestHandler<UpdateVehicleGroupCommand, Result<UpdatedVehicleGroupDTO>>
{
    public async Task<Result<UpdatedVehicleGroupDTO>> Handle(UpdateVehicleGroupCommand command, CancellationToken cancellationToken)
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
            return Result.Fail(
                ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        Guid currentUserId = tenantProvider.UserId.Value;

        User? currentUser = await userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser is null)
            return Result.Fail(
                ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            VehicleGroup? existingVehicleGroup = await vehicleGroupRepository.GetByIdAsync(command.VehicleGroupId);

            if (existingVehicleGroup is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.VehicleGroupId));            

            string formattedName = NameFormatter.FormatName(command.name);

            bool vehicleGroupNameAlreadyExists = await vehicleGroupRepository.ExistsByNameAsync(formattedName, command.VehicleGroupId);

            if (vehicleGroupNameAlreadyExists)
                return Result.Fail(
                    ErrorResults.InvalidRequestError("Já existe um grupo de veículos cadastrado com este nome para esta empresa."));           

            VehicleGroup updatedData= new VehicleGroup(
                formattedName,
                existingVehicleGroup.CompanyId
                );

            VehicleGroup updatedVehicleGroup = await vehicleGroupRepository.UpdateAsync(existingVehicleGroup, updatedData);
            
            await unitOfWork.CommitAsync();

            UpdatedVehicleGroupDTO vehicleGroupDto = mapper.Map<UpdatedVehicleGroupDTO>(updatedVehicleGroup);

            return Result.Ok(vehicleGroupDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização do grupo de veículos {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}