using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.BillingPlanModule.DTOs;
using OblivionDrive.Application.BillingPlanModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;

namespace OblivionDrive.Application.BillingPlanModule.Handlers;
public class GetBillingPlanByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryBillingPlan billingPlanRepository,
    IMapper mapper, ILogger<GetBillingPlanByIdHandler> logger, IValidator<GetBillingPlanByIdQuery> validator)
    : IRequestHandler<GetBillingPlanByIdQuery, Result<DetailBillingPlanDTO>>
{
    public async Task<Result<DetailBillingPlanDTO>> Handle(GetBillingPlanByIdQuery query, CancellationToken cancellationToken)
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
            BillingPlan? billingPlan = await billingPlanRepository.GetByIdAsync(query.BillingPlanId);

            if (billingPlan is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.BillingPlanId));

            if (billingPlan.CompanyId != currentCompanyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido visualizar planos de cobrança de outra empresa."));

            DetailBillingPlanDTO billingPlanDetail = mapper.Map<DetailBillingPlanDTO>(billingPlan);

            return Result.Ok(billingPlanDetail);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do plano de cobrança {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}