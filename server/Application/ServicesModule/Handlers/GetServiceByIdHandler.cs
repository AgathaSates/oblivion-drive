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
public sealed class GetServiceByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryServices serviceRepository,
    IMapper mapper, ILogger<GetServiceByIdHandler> logger, IValidator<GetServiceByIdQuery> validator)
    : IRequestHandler<GetServiceByIdQuery, Result<DetailServiceDTO>>
{
    public async Task<Result<DetailServiceDTO>> Handle(GetServiceByIdQuery query, CancellationToken cancellationToken)
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
            Service? service = await serviceRepository.GetByIdAsync(query.ServiceId);

            if (service is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.ServiceId));

            if (service.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido visualizar serviços de outra empresa."));

            DetailServiceDTO serviceDetail = mapper.Map<DetailServiceDTO>(service);

            return Result.Ok(serviceDetail);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do serviço {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}