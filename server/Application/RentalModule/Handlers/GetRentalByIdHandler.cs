using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Application.RentalModule.Handlers;
public class GetRentalByIdHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryRental rentalRepository,
    IMapper mapper, ILogger<GetRentalByIdHandler> logger, IValidator<GetRentalByIdQuery> validator)
    : IRequestHandler<GetRentalByIdQuery, Result<DetailRentalDTO>>
{
    public async Task<Result<DetailRentalDTO>> Handle(GetRentalByIdQuery query, CancellationToken cancellationToken)
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

        User? currentUser =
            await userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Rental? rental = await rentalRepository.GetByIdAsync(query.RentalId);

            if (rental is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.RentalId));

            if (rental.CompanyId != currentCompanyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido visualizar aluguéis de outra empresa."));

            DetailRentalDTO rentalDetail = mapper.Map<DetailRentalDTO>(rental);

            return Result.Ok(rentalDetail);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a obtenção de detalhes do aluguel {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}