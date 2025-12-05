using System.Collections.Immutable;
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

public class GetAllBillingPlanHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryBillingPlan billingPlanRepository,
    IMapper mapper, ILogger<GetAllBillingPlanHandler> logger, IValidator<GetAllBillingPlanQuery> validator)
    : IRequestHandler<GetAllBillingPlanQuery, Result<BillingPlanResult>>
{
    public async Task<Result<BillingPlanResult>> Handle(GetAllBillingPlanQuery query, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(query, cancellationToken);

        if (!validationResult.IsValid)
        {
            List<string> validationErrors = validationResult.Errors
                .Select(error => error.ErrorMessage)
                .ToList();

            return Result.Fail(
                ErrorResults.InvalidRequestError(validationErrors));
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
            IReadOnlyCollection<BillingPlan> billingPlans = query.Quantity.HasValue
                ? await billingPlanRepository.GetAllAsync(query.Quantity.Value)
                : await billingPlanRepository.GetAllAsync();

            List<DetailBillingPlanDTO> detailBillingPlans = mapper.Map<List<DetailBillingPlanDTO>>(billingPlans);

            BillingPlanResult result = new(detailBillingPlans.ToImmutableList());

            return Result.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de planos de cobrança da empresa {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}