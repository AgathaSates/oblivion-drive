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
public class GetPartnerByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryPartner partnerRepository,
    IMapper mapper, ILogger<GetPartnerByIdHandler> logger, IValidator<GetPartnerByIdQuery> validator)
    : IRequestHandler<GetPartnerByIdQuery, Result<DetailPartnerDTO>>
{
    public async Task<Result<DetailPartnerDTO>> Handle(GetPartnerByIdQuery query, CancellationToken cancellationToken)
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
            return Result.Fail(
                ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        Guid currentUserId = tenantProvider.UserId.Value;

        User? currentUser = await userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Partner? partner = await partnerRepository.GetByIdAsync(query.PartnerId);

            if (partner is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.PartnerId));

            if (partner.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido visualizar parceiros de outra empresa."));

            DetailPartnerDTO dto = mapper.Map<DetailPartnerDTO>(partner);

            return Result.Ok(dto);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do parceiro {@Query}.",
                query);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}