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

public class RegisterClientHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryClient clientRepository,
    IUnitOfWork unitOfWork, IValidator<CreateClientCommand> validator, ILogger<CreateClientCommand> logger,
    IMapper mapper) : IRequestHandler<CreateClientCommand, Result<ClientDTO>>
{
    public async Task<Result<ClientDTO>> Handle(CreateClientCommand command, CancellationToken cancellationToken)
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

            bool emailAlreadyExists = await clientRepository.ExistsByEmailAsync(command.Email);

            if (emailAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este e-mail."));


            bool phoneAlreadyExists = await clientRepository.ExistsByPhoneNumberAsync(command.PhoneNumber);

            if (phoneAlreadyExists)
                return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este telefone."));


            if (command.ClientType == ClientType.Individual)
            {
                if (!string.IsNullOrWhiteSpace(command.Cpf))
                {
                    bool cpfAlreadyExists =
                        await clientRepository.ExistsByCpfAsync(command.Cpf);

                    if (cpfAlreadyExists)
                        return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este CPF."));
                }

                if (!string.IsNullOrWhiteSpace(command.Rg))
                {
                    bool rgAlreadyExists =
                        await clientRepository.ExistsByRgAsync(command.Rg);

                    if (rgAlreadyExists)
                        return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este RG."));
                }

                if (!string.IsNullOrWhiteSpace(command.Cnh))
                {
                    bool cnhAlreadyExists =  await clientRepository.ExistsByCnhAsync(command.Cnh);

                    if (cnhAlreadyExists)
                        return Result.Fail(ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com esta CNH."));
                }
            }

            if (command.ClientType == ClientType.LegalEntity &&
                !string.IsNullOrWhiteSpace(command.Cnpj))
            {
                bool cnpjAlreadyExists =
                    await clientRepository.ExistsByCnpjAsync(command.Cnpj);

                if (cnpjAlreadyExists)
                {
                    return Result.Fail(
                        ErrorResults.InvalidRequestError("Já existe um cliente cadastrado com este CNPJ."));
                }
            }

            Client client = CreateClient(command, companyId, formattedName);

            await clientRepository.AddAsync(client);
            await unitOfWork.CommitAsync();

            ClientDTO clientDto = mapper.Map<ClientDTO>(client);

            return Result.Ok(clientDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante o registro de cliente {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private static Client CreateClient(CreateClientCommand command, Guid companyId, string formattedName)
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