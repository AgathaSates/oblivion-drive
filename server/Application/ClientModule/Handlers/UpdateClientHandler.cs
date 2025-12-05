using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Application.ClientModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.ClientModule.Handlers;
public class UpdateClientHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryClient clientRepository,
    IUnitOfWork unitOfWork, IValidator<UpdateClientCommand> validator, ILogger<UpdateClientCommand> logger,
    IMapper mapper) : IRequestHandler<UpdateClientCommand, Result<UpdatedClientDTO>>
{
    public async Task<Result<UpdatedClientDTO>> Handle(UpdateClientCommand command, CancellationToken cancellationToken)
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
            Client? existingClient = await clientRepository.GetByIdAsync(command.ClientId);

            if (existingClient is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.ClientId));

            if (existingClient.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido editar clientes de outra empresa."));

            bool emailAlreadyExists = await clientRepository.ExistsByEmailAsync(command.Email, command.ClientId);

            if (emailAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este e-mail."));

            bool phoneAlreadyExists = await clientRepository.ExistsByPhoneNumberAsync(command.PhoneNumber, command.ClientId);

            if (phoneAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este telefone."));

            if (!string.IsNullOrWhiteSpace(command.Cpf))
            {
                bool cpfAlreadyExists = await clientRepository.ExistsByCpfAsync(command.Cpf, command.ClientId);

                if (cpfAlreadyExists)
                    return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este CPF."));
                
            }

            if (!string.IsNullOrWhiteSpace(command.Rg))
            {
                bool rgAlreadyExists = await clientRepository.ExistsByRgAsync(command.Rg, command.ClientId);

                if (rgAlreadyExists)
                    return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este RG."));
            }

            if (!string.IsNullOrWhiteSpace(command.Cnh))
            {
                bool cnhAlreadyExists = await clientRepository.ExistsByCnhAsync(command.Cnh, command.ClientId);

                if (cnhAlreadyExists)
                    return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com esta CNH."));
            }

            if (!string.IsNullOrWhiteSpace(command.Cnpj))
            {
                bool cnpjAlreadyExists = await clientRepository.ExistsByCnpjAsync(command.Cnpj, command.ClientId);

                if (cnpjAlreadyExists)
                    return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este CNPJ."));
            }

            string formattedName = NameFormatter.FormatName(command.Name);

            Client updatedData = CreateUpdatedClient(command, existingClient.CompanyId, formattedName);

            Client updatedClient = await clientRepository.UpdateAsync(existingClient, updatedData);

            await unitOfWork.CommitAsync();

            UpdatedClientDTO dto = mapper.Map<UpdatedClientDTO>(updatedClient);

            return Result.Ok(dto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização do cliente {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private static Client CreateUpdatedClient(UpdateClientCommand command, Guid companyId, string formattedName)
    {
        Address address = new(
            state: command.State,
            city: command.City,
            district: command.District,
            street: command.Street,
            number: command.Number);

        return new Client(
            companyId: companyId,
            name: formattedName,
            email: command.Email,
            phoneNumber: command.PhoneNumber,
            clientType: command.ClientType,
            address: address,
            cpf: command.Cpf,
            rg: command.Rg,
            cnh: command.Cnh,
            cnpj: command.Cnpj);
    }
}