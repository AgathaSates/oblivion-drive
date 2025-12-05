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
public class GetDriverByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryDriver driverRepository,
    IMapper mapper, ILogger<GetDriverByIdHandler> logger, IValidator<GetDriverByIdQuery> validator)
    : IRequestHandler<GetDriverByIdQuery, Result<DetailDriverDTO>>
{
    public async Task<Result<DetailDriverDTO>> Handle(GetDriverByIdQuery query, CancellationToken cancellationToken)
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

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Driver? driver = await driverRepository.GetByIdAsync(query.DriverId);

            if (driver is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.DriverId));

            if (driver.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido visualizar condutores de outra empresa."));

            DetailDriverDTO driverDetail = mapper.Map<DetailDriverDTO>(driver);

            return Result.Ok(driverDetail);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do condutor {@Query}.",
                query);

            return Result.Fail(
                ErrorResults.InternalExceptionError(exception));
        }
    }
}