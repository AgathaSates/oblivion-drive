using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.ServicesModule.Commands;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.ServicesModule.Handlers;
public sealed class DeleteServiceHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider,
    IRepositoryServices serviceRepository, IRepositoryRental rentalRepository,
    IValidator<DeleteServiceCommand> validator, IUnitOfWork unitOfWork, ILogger<DeleteServiceHandler> logger)
    : IRequestHandler<DeleteServiceCommand, Result>
{
    public async Task<Result> Handle(DeleteServiceCommand command, CancellationToken cancellationToken)
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
            Service? service = await serviceRepository.GetByIdAsync(command.ServiceId);

            if (service is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.ServiceId));

            if (service.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido excluir serviços de outra empresa."));

            bool serviceUsedInOpenRental = await rentalRepository.ExistsOpenRentalUsingServiceAsync(service.Id);

            if (serviceUsedInOpenRental)
                return Result.Fail(
                    ErrorResults.InvalidRequestError(
                        "Não é permitido excluir serviços que estejam relacionados a aluguéis em andamento."));

            await serviceRepository.DeleteAsync(service);
            await unitOfWork.CommitAsync();

            return Result.Ok();
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a exclusão de serviço {@Command}.",
                command
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}
