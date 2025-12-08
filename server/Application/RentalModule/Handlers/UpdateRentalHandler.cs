using AutoMapper;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Application.RentalModule.Handlers;
public class UpdateRentalHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryRental rentalRepository,
    IRepositoryClient clientRepository, IRepositoryDriver driverRepository, IRepositoryVehicle vehicleRepository,
    IRepositoryBillingPlan billingPlanRepository, IRepositoryServices serviceRepository, IUnitOfWork unitOfWork,
    IValidator<UpdateRentalCommand> validator, ILogger<UpdateRentalCommand> logger, IMapper mapper,
    RentalPricingCalculator rentalPricingCalculator)
    : IRequestHandler<UpdateRentalCommand, Result<UpdatedRentalDTO>>
{
    public async Task<Result<UpdatedRentalDTO>> Handle(UpdateRentalCommand command, CancellationToken cancellationToken)
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
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Rental? existingRental = await rentalRepository.GetByIdAsync(command.RentalId);

            if (existingRental is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.RentalId));

            if (existingRental.CompanyId != companyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido editar aluguéis de outra empresa."));

            if (existingRental.IsCompleted)
                return Result.Fail(
                    ErrorResults.InvalidRequestError("Não é possível editar um aluguel já concluído."));

            Client? client = await clientRepository.GetByIdAsync(command.ClientId);

            if (client is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.ClientId));

            if (client.CompanyId != companyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido registrar aluguéis para clientes de outra empresa."));

            Driver? driver = await driverRepository.GetByIdAsync(command.DriverId);

            if (driver is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.DriverId));

            if (driver.CompanyId != companyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido registrar aluguéis com condutores de outra empresa."));

            if (driver.CnhExpirationDate < command.StartDate)
                return Result.Fail(
                    ErrorResults.InvalidRequestError("A CNH do condutor está vencida para a data de saída informada."));

            Vehicle? vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId);

            if (vehicle is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.VehicleId));

            if (vehicle.CompanyId != companyId)
                return Result.Fail( ErrorResults.UnauthorizedError("Não é permitido registrar aluguéis com veículos de outra empresa."));

            if (client.ClientType == ClientType.LegalEntity && driver.IsClientAlsoDriver)
                return Result.Fail(
                    ErrorResults.InvalidRequestError(
                        "Clientes do tipo pessoa jurídica não podem ser cadastrados como condutor. " +
                        "Selecione um condutor pessoa física vinculado a este cliente."));

            if (client.ClientType == ClientType.LegalEntity && driver.ClientId != client.Id)
                return Result.Fail(
                    ErrorResults.InvalidRequestError(
                        "O condutor selecionado deve estar vinculado ao cliente pessoa jurídica informado."));

            if (command.VehicleId != existingRental.VehicleId)
            {
                bool vehicleHasOpenRental =
                    await rentalRepository.ExistsOpenRentalForVehicleAsync(command.VehicleId);

                if (vehicleHasOpenRental)
                    return Result.Fail(
                        ErrorResults.InvalidRequestError("Este veículo não está disponível para locação."));
            }

            BillingPlan? billingPlan = await billingPlanRepository.GetByVehicleGroupIdAsync(vehicle.VehicleGroupId);

            if (billingPlan is null)
                return Result.Fail(
                    ErrorResults.InvalidRequestError("Não foi encontrado um plano de cobrança para o grupo do veículo selecionado."));      

            Result<IReadOnlyCollection<Service>> servicesResult =
                await LoadSelectedServicesAsync(command.ServiceIds, companyId);

            if (servicesResult.IsFailed)
                return Result.Fail<UpdatedRentalDTO>(servicesResult.Errors);

            IReadOnlyCollection<Service> selectedServices = servicesResult.Value;

            int rentalDays = rentalPricingCalculator.CalculateRentalDays(
                command.StartDate,
                command.ExpectedReturnDate);

            // Plano
            decimal rentalBasePrice =
                rentalPricingCalculator.CalculateEstimatedDailyRentalAmount(
                    billingPlan,
                    command.PlanType,
                    command.StartDate,
                    command.ExpectedReturnDate);

            // Seguro
            decimal insuranceTotalPrice =
                rentalPricingCalculator.CalculateInsuranceTotalPrice(
                    command.InsuranceDailyPricePerPerson,
                    command.InsurancePersonsCount,
                    rentalDays);

            // Serviços
            decimal servicesTotalPrice =
                rentalPricingCalculator.CalculateServicesTotalPrice(
                    selectedServices,
                    rentalDays);

            // Valor previsto (plano + seguro + serviços)
            decimal estimatedRentalAmount =
                rentalPricingCalculator.CalculateOnCreation(
                    rentalBasePrice,
                    insuranceTotalPrice,
                    servicesTotalPrice);

            Rental updatedData = CreateUpdatedRental(
                command,
                companyId,
                rentalBasePrice,
                insuranceTotalPrice,
                servicesTotalPrice,
                estimatedRentalAmount);

            Rental updatedRental = await rentalRepository.UpdateAsync(existingRental, updatedData);

            await unitOfWork.CommitAsync();

            UpdatedRentalDTO rentalDto = mapper.Map<UpdatedRentalDTO>(updatedRental);

            return Result.Ok(rentalDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a atualização de aluguel {@Command}.",
                command);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }

    private static Rental CreateUpdatedRental(
        UpdateRentalCommand command, Guid companyId, decimal rentalBasePrice,
        decimal insuranceTotalPrice, decimal servicesTotalPrice, decimal estimatedRentalAmount)
    {
        int estimatedTotalKilometersForConstructor = command.EstimatedTotalKilometers ?? 0;

        return new Rental(
            companyId,
            command.ClientId,
            command.DriverId,
            command.VehicleId,
            command.PlanType,
            command.StartDate,
            command.ExpectedReturnDate,
            command.InsuranceDailyPricePerPerson,
            command.InsurancePersonsCount,
            estimatedTotalKilometersForConstructor,
            servicesTotalPrice,
            insuranceTotalPrice,
            rentalBasePrice,
            estimatedRentalAmount,
            command.ServiceIds);
    }

    private async Task<Result<IReadOnlyCollection<Service>>> LoadSelectedServicesAsync(
        IReadOnlyCollection<Guid> serviceIds, Guid companyId)
    {
        if (serviceIds is null || serviceIds.Count == 0)
            return Result.Ok<IReadOnlyCollection<Service>>(Array.Empty<Service>());

        var services = new List<Service>(serviceIds.Count);

        foreach (Guid serviceId in serviceIds)
        {
            Service? service = await serviceRepository.GetByIdAsync(serviceId);

            if (service is null)
            {
                return Result.Fail<IReadOnlyCollection<Service>>(
                    ErrorResults.RecordNotFoundError(serviceId));
            }

            if (service.CompanyId != companyId)
            {
                return Result.Fail<IReadOnlyCollection<Service>>(
                    ErrorResults.UnauthorizedError("Não é permitido utilizar serviços de outra empresa."));
            }

            services.Add(service);
        }

        return Result.Ok<IReadOnlyCollection<Service>>(services);
    }
}
