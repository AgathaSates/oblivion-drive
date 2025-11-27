using System.Collections.Immutable;
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
public sealed class GetAllEmployeesForCompanyHandler(UserManager<User> userManager,
    ITenantProvider tenantProvider, IRepositoryEmployee employeeRepository,IMapper mapper,
    ILogger<GetAllEmployeesForCompanyHandler> logger, IValidator<GetAllEmployeesForCompanyQuery> validator)
    : IRequestHandler<GetAllEmployeesForCompanyQuery, Result<EmployeesResult>>
{
    public async Task<Result<EmployeesResult>> Handle(GetAllEmployeesForCompanyQuery query, CancellationToken cancellationToken)
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
            return Result.Fail(ErrorResults.UnauthorizedError("Apenas usuários do tipo empresa podem listar funcionários."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            IReadOnlyCollection<Employee> employees = query.Quantity.HasValue
                ? await employeeRepository.GetAllAsync(query.Quantity.Value)
                : await employeeRepository.GetAllAsync();

            List<DetailEmployeeDTO> detailEmployees =
                mapper.Map<List<DetailEmployeeDTO>>(employees);

            EmployeesResult employeesResult = new EmployeesResult(detailEmployees.ToImmutableList());

            return Result.Ok(employeesResult);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de funcionários da empresa {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}