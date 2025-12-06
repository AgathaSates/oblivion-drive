using System.Collections.Immutable;
using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.PartnerModule.DTOs;
using OblivionDrive.Application.PartnerModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.PartnerModule;

namespace OblivionDrive.Application.PartnerModule.Handlers;
public class GetAllPartnersHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryPartner partnerRepository,
    IMapper mapper, ILogger<GetAllPartnersHandler> logger, IValidator<GetAllPartnersQuery> validator)
    : IRequestHandler<GetAllPartnersQuery, Result<PartnersResult>>
{
    public async Task<Result<PartnersResult>> Handle(GetAllPartnersQuery query, CancellationToken cancellationToken)
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
            IReadOnlyCollection<Partner> partners = query.Quantity.HasValue
                ? await partnerRepository.GetAllAsync(query.Quantity.Value)
                : await partnerRepository.GetAllAsync();

            List<DetailPartnerDTO> partnerDtos = mapper.Map<List<DetailPartnerDTO>>(partners);

            PartnersResult result = new(partnerDtos.ToImmutableList());

            return Result.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de parceiros da empresa {@Query}.",
                query);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}