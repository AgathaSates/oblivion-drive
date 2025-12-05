using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.ClientModule.DTOs;
using OblivionDrive.Application.ClientModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Application.ClientModule.Handlers;
public sealed class GetClientByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryClient clientRepository,
    IMapper mapper, ILogger<GetClientByIdHandler> logger, IValidator<GetClientByIdQuery> validator)
    : IRequestHandler<GetClientByIdQuery, Result<DetailClientDTO>>
{
    public async Task<Result<DetailClientDTO>> Handle(GetClientByIdQuery query, CancellationToken cancellationToken)
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
            Client? client = await clientRepository.GetByIdAsync(query.ClientId);

            if (client is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.ClientId));

            if (client.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido visualizar clientes de outra empresa."));

            DetailClientDTO clientDetail = mapper.Map<DetailClientDTO>(client);

            return Result.Ok(clientDetail);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do cliente {@Query}.",
                query);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}