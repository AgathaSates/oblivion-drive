using System.Collections.Immutable;
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
public class GetAllClientsHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryClient clientRepository,
    IMapper mapper, ILogger<GetAllClientsHandler> logger, IValidator<GetAllClientsQuery> validator)
    : IRequestHandler<GetAllClientsQuery, Result<ClientsResult>>
{
    public async Task<Result<ClientsResult>> Handle(GetAllClientsQuery query, CancellationToken cancellationToken)
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
            IReadOnlyCollection<Client> clients = query.Quantity.HasValue
                ? await clientRepository.GetAllAsync(query.Quantity.Value)
                : await clientRepository.GetAllAsync();

            List<DetailClientDTO> clientDtos = mapper.Map<List<DetailClientDTO>>(clients);

            ClientsResult result = new(clientDtos.ToImmutableList());

            return Result.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de clientes da empresa {@Query}.",
                query);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}