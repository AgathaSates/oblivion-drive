using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.AuthenticationModule.Extensions;
using OblivionDrive.Application.EmployeeModule.Commands;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Application.EmployeeModule.Handlers;
public class RegisterEmployeeHandler(
    UserManager<User> userManager, IValidator<RegisterEmployeeCommand> validator,
    ITenantProvider tenantProvider, IRepositoryEmployee employeeRepository,
    ILogger<RegisterEmployeeCommand> logger, IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<RegisterEmployeeCommand, Result<EmployeeDTO>>
{
    public async Task<Result<EmployeeDTO>> Handle(RegisterEmployeeCommand command, CancellationToken cancellationToken)
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
            return Result.Fail(ErrorResults.UnauthorizedError("Apenas usuários do tipo empresa podem cadastrar funcionários."));

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Guid employeeId = Guid.NewGuid();

            User employeeUser = CreateEmployeeIdentityUser(command, companyId, currentUser, employeeId);

            IdentityResult userResult = await userManager.CreateAsync(employeeUser, command.Password);

            if (!userResult.Succeeded)
                return userResult.ToInvalidRequestResult<EmployeeDTO>();

            IdentityResult roleResult = await userManager.AddToRoleAsync(
                employeeUser,
                UserType.Employee.ToString()
            );

            if (!roleResult.Succeeded)
                return roleResult.ToInvalidRequestResult<EmployeeDTO>();

            Employee employee = CreateEmployeeEntity(command, companyId, employeeUser, employeeId);

            await employeeRepository.AddAsync(employee);
            await unitOfWork.CommitAsync();

            EmployeeDTO employeeDto = mapper.Map<EmployeeDTO>(employee);

            return Result.Ok(employeeDto);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                ex,
                "Ocorreu um erro durante o registro de funcionário {@Command}.",
                command
            );

            return Result.Fail(ErrorResults.InternalExceptionError(ex));
        }
    }

    private User CreateEmployeeIdentityUser(RegisterEmployeeCommand command, Guid companyId, User companyUser, Guid emplooyeId)
    {
        return new User
        {
            UserName = command.UserName,
            Email = command.Email,
            UserType = UserType.Employee,
            CompanyId = companyId,
            CompanyUser = companyUser,
            EmployeeID = emplooyeId
        };
    }

    private Employee CreateEmployeeEntity(RegisterEmployeeCommand command, Guid companyId, User employeeUser, Guid emplooyeId)
    {
        string formattedEmployeeName = NameFormatter.FormatName(command.Name);
        return new Employee(
            emplooyeId,
            companyId,
            employeeUser,
            employeeUser.Id,
            formattedEmployeeName,
            command.HireDate,
            command.Salary
        );
    }
}