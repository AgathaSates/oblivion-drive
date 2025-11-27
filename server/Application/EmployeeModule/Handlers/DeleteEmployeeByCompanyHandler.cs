using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.EmployeeModule.Handlers;
public class DeleteEmployeeByCompanyHandler(UserManager<User> userManager,
    ITenantProvider tenantProvider, IRepositoryEmployee employeeRepository,
    IValidator<DeleteEmployeeByCompanyCommand> validator, IUnitOfWork unitOfWork,
    ILogger<DeleteEmployeeByCompanyHandler> logger) : IRequestHandler<DeleteEmployeeByCompanyCommand, Result>
{
    public async Task<Result> Handle(DeleteEmployeeByCompanyCommand command, CancellationToken cancellationToken)
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

        if (currentUser.UserType != UserType.Company)
            return Result.Fail(ErrorResults.UnauthorizedError("Apenas usuários do tipo empresa podem excluir funcionários."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Employee? employee = await employeeRepository.GetByIdAsync(command.EmployeeId);

            if (employee is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.EmployeeId));

            if (employee.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido excluir funcionários de outra empresa."));

            User? employeeUser = null;

            if (employee.IdentityUserId != Guid.Empty)
            {
                employeeUser = await userManager.FindByIdAsync(employee.IdentityUserId.ToString());
            }

            await employeeRepository.DeleteAsync(employee);

            if (employeeUser is not null)
            {
                var deleteUserResult = await userManager.DeleteAsync(employeeUser);

                if (!deleteUserResult.Succeeded)
                {
                    List<string> errors = deleteUserResult.Errors
                        .Select(error => error.Description)
                        .ToList();

                    return Result.Fail(ErrorResults.InvalidRequestError(errors));
                }
            }

            await unitOfWork.CommitAsync();

            return Result.Ok();
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a exclusão de funcionário {@Command}.",
                command
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}