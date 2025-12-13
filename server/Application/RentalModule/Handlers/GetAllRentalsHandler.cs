using System.Collections.Immutable;
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
public class GetAllRentalsHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryRental rentalRepository,
    IMapper mapper, ILogger<GetAllRentalsHandler> logger, IValidator<GetAllRentalsQuery> validator)
    : IRequestHandler<GetAllRentalsQuery, Result<RentalsResult>>
{
    public async Task<Result<RentalsResult>> Handle(GetAllRentalsQuery query, CancellationToken cancellationToken)
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
            IReadOnlyCollection<Rental> rentals = query.Quantity.HasValue
                ? await rentalRepository.GetAllAsync(query.Quantity.Value)
                : await rentalRepository.GetAllAsync();

            List<DetailRentalDTO> detailRentals = mapper.Map<List<DetailRentalDTO>>(rentals);

            RentalsResult result = new(detailRentals.ToImmutableList());

            return Result.Ok(result);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro durante a listagem de aluguéis da empresa {@Query}.",
                query
            );

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}