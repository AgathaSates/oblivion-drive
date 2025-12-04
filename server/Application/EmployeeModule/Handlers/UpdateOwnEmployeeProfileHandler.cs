using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.EmployeeModule.Handlers;
public class UpdateOwnEmployeeProfileHandler(UserManager<User> userManager,
    ITenantProvider tenantProvider, IRepositoryEmployee employeeRepository,
    IValidator<UpdateOwnEmployeeProfileCommand> validator, IUnitOfWork unitOfWork,
    IMapper mapper, ILogger<UpdateOwnEmployeeProfileHandler> logger)
    : IRequestHandler<UpdateOwnEmployeeProfileCommand, Result<UpdatedEmployeeDTO>>
{
    public async Task<Result<UpdatedEmployeeDTO>> Handle(UpdateOwnEmployeeProfileCommand command, CancellationToken cancellationToken)
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

        if (currentUser.UserType != UserType.Employee)
            return Result.Fail(ErrorResults.UnauthorizedError("Apenas usuários do tipo funcionário podem editar o próprio perfil."));

        try
        {
            Employee? employee = await employeeRepository.GetByIdAsync((Guid)currentUser.EmployeeID!);

            if (employee is null)
                return Result.Fail(ErrorResults.RecordNotFoundError("Funcionário associado ao usuário não foi encontrado."));

            if (employee.IdentityUserId != currentUserId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido editar o perfil de outro funcionário."));

            string formattedName = NameFormatter.FormatName(command.Name);


            if (!string.Equals(employee.Name, formattedName, StringComparison.CurrentCultureIgnoreCase))
            {
                bool duplicatedNameExists =
                    await employeeRepository.ExistsByNameAsync(command.Name, employee.Id);

                if (duplicatedNameExists)
                {
                    return Result.Fail(
                        ErrorResults.InvalidRequestError("Já existe um funcionário cadastrado com este nome para esta empresa."));
                }
            }

            await employeeRepository.UpdateOwnProfileNameAsync(employee, formattedName);
            await unitOfWork.CommitAsync();

            UpdatedEmployeeDTO updatedEmployeeDto = mapper.Map<UpdatedEmployeeDTO>(employee);

            return Result.Ok(updatedEmployeeDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização do perfil do funcionário {@Command}.",
                command
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}