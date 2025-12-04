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
public class UpdateEmployeeByCompanyHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider,
    IRepositoryEmployee employeeRepository, IValidator<UpdateEmployeeByCompanyCommand> validator,
    IUnitOfWork unitOfWork, IMapper mapper, ILogger<UpdateEmployeeByCompanyHandler> logger)
    : IRequestHandler<UpdateEmployeeByCompanyCommand, Result<UpdatedEmployeeDTO>>
{
    public async Task<Result<UpdatedEmployeeDTO>> Handle(
        UpdateEmployeeByCompanyCommand command, CancellationToken cancellationToken)
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
            return Result.Fail(ErrorResults.UnauthorizedError("Apenas usuários do tipo empresa podem editar funcionários."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Employee? employee = await employeeRepository.GetByIdAsync(command.EmployeeId);

            if (employee is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.EmployeeId));

            if (employee.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido editar funcionários de outra empresa."));

            string formattedName = NameFormatter.FormatName(command.Name);

            if (!string.Equals(employee.Name, formattedName, StringComparison.CurrentCultureIgnoreCase))
            {
                bool nameAlreadyExists =
                    await employeeRepository.ExistsByNameAsync(command.Name, employee.Id);

                if (nameAlreadyExists)
                {
                    return Result.Fail(
                        ErrorResults.InvalidRequestError("Já existe um funcionário cadastrado com este nome para esta empresa."));
                }
            }

            Employee updateEntity = CreateUpdatedEmployee(command);

            await employeeRepository.UpdateAsync(employee, updateEntity);
            await unitOfWork.CommitAsync();

            UpdatedEmployeeDTO updatedEmployeeDto = mapper.Map<UpdatedEmployeeDTO>(employee);

            return Result.Ok(updatedEmployeeDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização de funcionário {@Command}.",
                command
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private  Employee CreateUpdatedEmployee(UpdateEmployeeByCompanyCommand command)
    {
        string formattedName = NameFormatter.FormatName(command.Name);
        return new Employee(
            Guid.Empty,
            Guid.Empty,
            null!,
            Guid.Empty,
            formattedName,
            command.HireDate,
            command.Salary
        );
    }
}