using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.ServicesModule.Commands;
using OblivionDrive.Application.ServicesModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.ServicesModule.Handlers;

public sealed class RegisterServiceHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryServices serviceRepository,
    IUnitOfWork unitOfWork, IValidator<RegisterServiceCommand> validator, ILogger<RegisterServiceCommand> logger,
    IMapper mapper) : IRequestHandler<RegisterServiceCommand, Result<ServiceDTO>>
{
    public async Task<Result<ServiceDTO>> Handle(RegisterServiceCommand command, CancellationToken cancellationToken)
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

            bool serviceNameAlreadyExists =
                await serviceRepository.ExistsByNameAsync(formattedName);

            if (serviceNameAlreadyExists)
            {
                return Result.Fail(
                    ErrorResults.InvalidRequestError("Já existe um serviço cadastrado com este nome para esta empresa."));
            }

            Service service = CreateService(command, companyId);

            await serviceRepository.AddAsync(service);
            await unitOfWork.CommitAsync();

            ServiceDTO serviceDto = mapper.Map<ServiceDTO>(service);

            return Result.Ok(serviceDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante o registro de serviço {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private Service CreateService(RegisterServiceCommand command, Guid companyId)
    {
        string formattedName = NameFormatter.FormatName(command.Name);
        return new Service(
                formattedName,
                command.Price,
                command.ChargeType,
                companyId);
    }
}