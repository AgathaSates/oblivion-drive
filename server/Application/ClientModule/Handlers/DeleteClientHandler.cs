using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.ClientModule.Handlers;

public class DeleteClientHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider,
    IRepositoryClient clientRepository, IRepositoryRental rentalRepository,
    IValidator<DeleteClientCommand> validator, IUnitOfWork unitOfWork, ILogger<DeleteClientHandler> logger)
    : IRequestHandler<DeleteClientCommand, Result>
{
    public async Task<Result> Handle(DeleteClientCommand command, CancellationToken cancellationToken)
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
            Client? client = await clientRepository.GetByIdAsync(command.ClientId);

            if (client is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.ClientId));

            if (client.CompanyId != currentCompanyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido excluir clientes de outra empresa."));

            bool clientHasOpenRental =
               await rentalRepository.ExistsOpenRentalForClientAsync(client.Id);

            if (clientHasOpenRental)
                return Result.Fail(
                    ErrorResults.InvalidRequestError(
                        "Não é permitido excluir clientes que possuam aluguéis em andamento."));

            await clientRepository.DeleteAsync(client);
            await unitOfWork.CommitAsync();

            return Result.Ok();
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a exclusão de cliente {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}