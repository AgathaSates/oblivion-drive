using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.Shared;
using OblivionDrive.Application.VehicleModule.Commands;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Application.VehicleModule.Handlers;
public class DeleteVehicleHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider,
    IRepositoryVehicle vehicleRepository, IRepositoryRental rentalRepository,
    IValidator<DeleteVehicleCommand> validator, IUnitOfWork unitOfWork, ILogger<DeleteVehicleHandler> logger)
    : IRequestHandler<DeleteVehicleCommand, Result>
{
    public async Task<Result> Handle(DeleteVehicleCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, cancellationToken);

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
            return Result.Fail(
                ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Vehicle? vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId);

            if (vehicle is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.VehicleId));

            if (vehicle.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido excluir veículos de outra empresa."));

            bool vehicleHasOpenRental = await rentalRepository.ExistsOpenRentalForVehicleAsync(vehicle.Id);

            if (vehicleHasOpenRental)
                return Result.Fail(
                    ErrorResults.InvalidRequestError(
                        "Não é permitido excluir veículos que estejam vinculados a aluguéis em andamento."));

            await vehicleRepository.DeleteAsync(vehicle);
            await unitOfWork.CommitAsync();

            return Result.Ok();
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a exclusão de veículo {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
}
