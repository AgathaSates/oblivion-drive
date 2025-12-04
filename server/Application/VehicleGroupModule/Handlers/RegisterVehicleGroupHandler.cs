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
public class RegisterVehicleGroupHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider,IRepositoryVehicleGroup vehicleGroupRepository,
    IUnitOfWork unitOfWork, IValidator<RegisterVehicleGroupCommand> validator, 
    ILogger<RegisterVehicleGroupCommand> logger, IMapper mapper)
    : IRequestHandler<RegisterVehicleGroupCommand, Result<VehicleGroupDTO>>
{
    public async Task<Result<VehicleGroupDTO>> Handle(RegisterVehicleGroupCommand command, CancellationToken cancellationToken)
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
        
        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            string formattedName = NameFormatter.FormatName(command.Name);

            bool vehicleGroupNameAlreadyExists = await vehicleGroupRepository.ExistsByNameAsync(formattedName);

            if (vehicleGroupNameAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um grupo de veículos cadastrado com este nome para esta empresa."));          

            VehicleGroup vehicleGroup = CreateVehicleGroup(command, companyId);

            await vehicleGroupRepository.AddAsync(vehicleGroup);
            await unitOfWork.CommitAsync();

            VehicleGroupDTO vehicleGroupDto = mapper.Map<VehicleGroupDTO>(vehicleGroup);

            return Result.Ok(vehicleGroupDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante o registro de grupo de veículos {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private VehicleGroup CreateVehicleGroup(RegisterVehicleGroupCommand command, Guid companyId)
    {
        string formattedName = NameFormatter.FormatName(command.Name);

        return new VehicleGroup(
            formattedName,
            companyId);
    }
}