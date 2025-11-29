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
public sealed class UpdateServiceHandler(
    UserManager<User> userManager, IValidator<UpdateServiceCommand> validator, ITenantProvider tenantProvider,
    IRepositoryServices serviceRepository, IUnitOfWork unitOfWork, ILogger<UpdateServiceCommand> logger,
    IMapper mapper) : IRequestHandler<UpdateServiceCommand, Result<UpdatedServiceDTO>>
{
    public async Task<Result<UpdatedServiceDTO>> Handle(UpdateServiceCommand command, CancellationToken cancellationToken)
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

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Service? existingService = await serviceRepository.GetByIdAsync(command.ServiceId);

            if (existingService is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.ServiceId));

            if (existingService.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido editar serviços de outra empresa."));

            string formattedEmployeeName = NameFormatter.FormatName(command.Name);

            Service updatedData = new Service(
                formattedEmployeeName,
                command.Price,
                command.ChargeType,
                existingService.CompanyId);

            Service updatedService = await serviceRepository.UpdateAsync(existingService, updatedData);

            await unitOfWork.CommitAsync();

            UpdatedServiceDTO updatedServiceDto = mapper.Map<UpdatedServiceDTO>(updatedService);

            return Result.Ok(updatedServiceDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização de serviço {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}