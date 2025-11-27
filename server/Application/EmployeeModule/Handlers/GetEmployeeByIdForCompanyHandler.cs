using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.EmployeeModule.DTOs;
using OblivionDrive.Application.EmployeeModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;

namespace OblivionDrive.Application.EmployeeModule.Handlers;

public class GetEmployeeByIdForCompanyHandler(UserManager<User> userManager,
    ITenantProvider tenantProvider, IRepositoryEmployee employeeRepository,
    IValidator<GetEmployeeByIdForCompanyQuery> validator, IMapper mapper,
    ILogger<GetEmployeeByIdForCompanyHandler> logger) : IRequestHandler<GetEmployeeByIdForCompanyQuery, Result<DetailEmployeeDTO>>
{
    public async Task<Result<DetailEmployeeDTO>> Handle(GetEmployeeByIdForCompanyQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(query, cancellationToken);

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
            return Result.Fail(ErrorResults.UnauthorizedError("Apenas usuários do tipo empresa podem consultar funcionários."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Employee? employee = await employeeRepository.GetByIdAsync(query.EmployeeId);

            if (employee is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.EmployeeId));

            if (employee.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido consultar funcionários de outra empresa."));

            DetailEmployeeDTO employeeDto = mapper.Map<DetailEmployeeDTO>(employee);

            return Result.Ok(employeeDto);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a consulta de funcionário por Id {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}