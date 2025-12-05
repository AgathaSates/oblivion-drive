using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.DriverModule.Commands;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.DriverModule.Handlers;
public class DeleteDriverHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryDriver driverRepository,
    IValidator<DeleteDriverCommand> validator, IUnitOfWork unitOfWork, ILogger<DeleteDriverHandler> logger)
    : IRequestHandler<DeleteDriverCommand, Result>
{
    public async Task<Result> Handle(DeleteDriverCommand command, CancellationToken cancellationToken)
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
            Driver? driver = await driverRepository.GetByIdAsync(command.DriverId);

            if (driver is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.DriverId));

            if (driver.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido excluir condutores de outra empresa."));

            await driverRepository.DeleteAsync(driver);
            await unitOfWork.CommitAsync();

            return Result.Ok();
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a exclusão de condutor {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}
