using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.DriverModule.Commands;
using OblivionDrive.Application.DriverModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.DriverModule.Handlers;
public class UpdateDriverHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryDriver driverRepository,
    IRepositoryClient clientRepository, IUnitOfWork unitOfWork, IValidator<UpdateDriverCommand> validator,
    ILogger<UpdateDriverCommand> logger, IMapper mapper) : IRequestHandler<UpdateDriverCommand, Result<UpdatedDriverDTO>>
{
    public async Task<Result<UpdatedDriverDTO>> Handle(UpdateDriverCommand command, CancellationToken cancellationToken)
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
            Driver? existingDriver = await driverRepository.GetByIdAsync(command.DriverId);

            if (existingDriver is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.DriverId));

            if (existingDriver.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido editar condutores de outra empresa."));

            Client? client = await clientRepository.GetByIdAsync(command.ClientId);

            if (client is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.ClientId));

            if (client.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido vincular condutores a clientes de outra empresa."));

            bool emailAlreadyExists = await driverRepository.ExistsByEmailAsync(command.Email, command.DriverId);

            if (emailAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um condutor cadastrado com este e-mail."));

            bool phoneAlreadyExists = await driverRepository.ExistsByPhoneNumberAsync(command.PhoneNumber, command.DriverId);

            if (phoneAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um condutor cadastrado com este telefone."));


            bool cpfAlreadyExists = await driverRepository.ExistsByCpfAsync(command.Cpf, command.DriverId);

            if (cpfAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um condutor cadastrado com este CPF."));

            bool cnhAlreadyExists = await driverRepository.ExistsByCnhAsync(command.Cnh, command.DriverId);

            if (cnhAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um condutor cadastrado com esta CNH."));

            string formattedName = NameFormatter.FormatName(command.Name);

            Driver updatedData = CreateUpdatedDriver(command, companyId, formattedName);

            Driver updatedDriver = await driverRepository.UpdateAsync(existingDriver, updatedData);

            await unitOfWork.CommitAsync();

            UpdatedDriverDTO dto = mapper.Map<UpdatedDriverDTO>(updatedDriver);

            return Result.Ok(dto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização do condutor {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private static Driver CreateUpdatedDriver(UpdateDriverCommand command, Guid companyId, string formattedName)
    {
        return new Driver(
            name: formattedName,
            email: command.Email,
            phoneNumber: command.PhoneNumber,
            cpf: command.Cpf,
            cnh: command.Cnh,
            cnhExpirationDate: command.CnhExpirationDate,
            clientId: command.ClientId,
            companyId: companyId,
            isClientAlsoDriver: command.IsClientAlsoDriver);
    }
}