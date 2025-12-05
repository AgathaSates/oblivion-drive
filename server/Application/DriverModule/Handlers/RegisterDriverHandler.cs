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
public class RegisterDriverHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryDriver driverRepository,
    IRepositoryClient clientRepository, IUnitOfWork unitOfWork, IValidator<RegisterDriverCommand> validator,
    ILogger<RegisterDriverCommand> logger, IMapper mapper) : IRequestHandler<RegisterDriverCommand, Result<DriverDTO>>
{
    public async Task<Result<DriverDTO>> Handle(RegisterDriverCommand command, CancellationToken cancellationToken)
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
            Client? client = await clientRepository.GetByIdAsync(command.ClientId);

            if (client is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.ClientId));

            if (client.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido vincular condutores a clientes de outra empresa."));

            bool emailAlreadyExists = await driverRepository.ExistsByEmailAsync(command.Email);

            if (emailAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um condutor cadastrado com este e-mail."));

            bool phoneAlreadyExists = await driverRepository.ExistsByPhoneNumberAsync(command.PhoneNumber);

            if (phoneAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um condutor cadastrado com este telefone."));

            bool cpfAlreadyExists =
                await driverRepository.ExistsByCpfAsync(command.Cpf);

            if (cpfAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um condutor cadastrado com este CPF."));

            bool cnhAlreadyExists = await driverRepository.ExistsByCnhAsync(command.Cnh);

            if (cnhAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um condutor cadastrado com esta CNH."));

            string formattedName = NameFormatter.FormatName(command.Name);

            Driver driver = CreateDriver(command, companyId, formattedName);

            await driverRepository.AddAsync(driver);
            await unitOfWork.CommitAsync();

            DriverDTO dto = mapper.Map<DriverDTO>(driver);

            return Result.Ok(dto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante o registro de condutor {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private static Driver CreateDriver(RegisterDriverCommand command, Guid companyId, string formattedName)
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