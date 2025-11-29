using System.Collections.Immutable;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.ServicesModule.DTOs;
using OblivionDrive.Application.ServicesModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ServicesModule;

namespace OblivionDrive.Application.ServicesModule.Handlers;
public sealed class GetAllServicesHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryServices serviceRepository,
    IMapper mapper, ILogger<GetAllServicesHandler> logger, IValidator<GetAllServicesQuery> validator)
    : IRequestHandler<GetAllServicesQuery, Result<ServicesResult>>
{
    public async Task<Result<ServicesResult>> Handle(GetAllServicesQuery query, CancellationToken cancellationToken)
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
            IReadOnlyCollection<Service> services = query.Quantity.HasValue
                ? await serviceRepository.GetAllAsync(query.Quantity.Value)
                : await serviceRepository.GetAllAsync();

            List<DetailServiceDTO> detailServices = mapper.Map<List<DetailServiceDTO>>(services);

            ServicesResult servicesResult = new ServicesResult(detailServices.ToImmutableList());

            return Result.Ok(servicesResult);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de serviços da empresa {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}