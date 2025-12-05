using System.Collections.Immutable;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.DriverModule.DTOs;
using OblivionDrive.Application.DriverModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.DriverModule;

namespace OblivionDrive.Application.DriverModule.Handlers;
public class GetAllDriversHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryDriver driverRepository,
    IMapper mapper, ILogger<GetAllDriversHandler> logger, IValidator<GetAllDriversQuery> validator)
    : IRequestHandler<GetAllDriversQuery, Result<DriversResult>>
{
    public async Task<Result<DriversResult>> Handle(GetAllDriversQuery query, CancellationToken cancellationToken)
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
            return Result.Fail(
                ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            IReadOnlyCollection<Driver> drivers = query.Quantity.HasValue
                ? await driverRepository.GetAllAsync(query.Quantity.Value)
                : await driverRepository.GetAllAsync();

            List<DetailDriverDTO> driverDtos = mapper.Map<List<DetailDriverDTO>>(drivers);

            DriversResult result = new(driverDtos.ToImmutableList());

            return Result.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de condutores da empresa {@Query}.",
                query);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}
