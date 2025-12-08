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
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Application.RentalModule.Handlers;
public sealed class CompleteRentalReturnHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryRental rentalRepository,
    IRepositoryVehicle vehicleRepository, IRepositoryBillingPlan billingPlanRepository,
    IRepositoryServices serviceRepository, IRepositoryFuelPriceSettings fuelPriceConfigurationRepository,
    IRepositoryCoupon couponRepository, IUnitOfWork unitOfWork, IValidator<CompleteRentalReturnCommand> validator,
    ILogger<CompleteRentalReturnHandler> logger, RentalPricingCalculator rentalPricingCalculator)
    : IRequestHandler<CompleteRentalReturnCommand, Result<CompletedRentalDTO>>
{
    public async Task<Result<CompletedRentalDTO>> Handle(CompleteRentalReturnCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, cancellationToken);

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
            Rental? rental = await rentalRepository.GetByIdAsync(command.RentalId);

            if (rental is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.RentalId));

            if (rental.CompanyId != currentCompanyId)
                return Result.Fail(
                    ErrorResults.UnauthorizedError("Não é permitido concluir devoluções de aluguéis de outra empresa."));

            if (rental.IsCompleted)
                return Result.Fail(
                    ErrorResults.InvalidRequestError("Este aluguel já foi concluído e não pode ser devolvido novamente."));

            if (command.ActualReturnDate < rental.StartDate)
                return Result.Fail(ErrorResults.InvalidRequestError("A data de devolução não pode ser anterior à data de saída do aluguel."));

            Vehicle? vehicle = await vehicleRepository.GetByIdAsync(rental.VehicleId);

            if (vehicle is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(rental.VehicleId));

            if (vehicle.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido concluir devoluções com veículos de outra empresa."));

            BillingPlan? billingPlan = await billingPlanRepository.GetByVehicleGroupIdAsync(vehicle.VehicleGroupId);

            if (billingPlan is null)
                return Result.Fail(
                    ErrorResults.InvalidRequestError("Não foi encontrado um plano de cobrança para o grupo do veículo selecionado."));

            Result<IReadOnlyCollection<Service>> servicesResult =
                await LoadSelectedServicesAsync(rental.ServiceIds, currentCompanyId);

            if (servicesResult.IsFailed)
                return Result.Fail<CompletedRentalDTO>(servicesResult.Errors);

            IReadOnlyCollection<Service> selectedServices = servicesResult.Value;

            FuelPriceConfiguration? fuelPriceConfiguration = await fuelPriceConfigurationRepository.GetAsync(currentCompanyId);

            if (fuelPriceConfiguration is null)
                return Result.Fail<CompletedRentalDTO>(
                    ErrorResults.InvalidRequestError(
                        "Não foi possível localizar a configuração de preço de combustível da empresa."));

            int rentalDays = rentalPricingCalculator.CalculateRentalDays(
                rental.StartDate,
                command.ActualReturnDate);

            // Plano
            decimal rentalBasePrice =
                rentalPricingCalculator.CalculateFinalRentalAmountOnReturn(
                    billingPlan,
                    rental.PlanType,
                    rental.StartDate,
                    command.ActualReturnDate,
                    command.InitialOdometerInKm,
                    command.CurrentOdometerInKm,
                    rental.EstimatedTotalKilometers);

            // Seguro
            decimal insuranceTotalPrice =
                rentalPricingCalculator.CalculateInsuranceTotalPrice(
                    rental.InsuranceDailyPricePerPerson,
                    rental.InsurancePersonsCount,
                    rentalDays);

            // Serviços
            decimal servicesTotalPrice =
                rentalPricingCalculator.CalculateServicesTotalPrice(
                    selectedServices,
                    rentalDays);

            // Combustíve
            decimal fuelChargePrice = 0m;

            if (!command.IsFuelTankFullOnReturn)
            {
                decimal configuredPriceForFuelType = vehicle.FuelType switch
                {
                    FuelType.Gasoline => fuelPriceConfiguration.Gasoline,
                    FuelType.Gas => fuelPriceConfiguration.Gas,
                    FuelType.Diesel => fuelPriceConfiguration.Diesel,
                    FuelType.Alcohol => fuelPriceConfiguration.Alcohol,
                    _ => 0m
                };

                if (configuredPriceForFuelType <= 0m)
                {
                    return Result.Fail<CompletedRentalDTO>(
                        ErrorResults.InvalidRequestError(
                            "Os valores de combustível ainda não foram configurados para o tipo de combustível deste veículo. " +
                            "Atualize a tabela de preços de combustível antes de concluir a devolução."));
                }

                fuelChargePrice = rentalPricingCalculator.CalculateFuelChargePrice(
                    vehicle,
                    fuelPriceConfiguration,
                    isFuelTankFullOnReturn: command.IsFuelTankFullOnReturn);
            }

            // Multa por atraso
            decimal penaltyPrice =
                rentalPricingCalculator.CalculateLateReturnPenalty(
                    rental.ExpectedReturnDate,
                    command.ActualReturnDate,
                    rentalBasePrice);

            // Soma bruta: plano + seguro + serviços + combustível + multa
            decimal grossRentalAmount =
                rentalPricingCalculator.CalculateOnReturn(
                    rentalBasePrice,
                    insuranceTotalPrice,
                    servicesTotalPrice,
                    fuelChargePrice,
                    penaltyPrice);

            Guid? couponId = null;
            decimal couponDiscountAmount = 0m;

            if (!string.IsNullOrWhiteSpace(command.CouponName))
            {
                string normalizedCouponName = command.CouponName.Trim().ToUpperInvariant();

                Coupon? coupon = await couponRepository.GetByNameAsync(normalizedCouponName);

                if (coupon is null)
                    return Result.Fail<CompletedRentalDTO>(
                        ErrorResults.InvalidRequestError("O cupom informado não existe."));

                if (coupon.CompanyId != currentCompanyId)
                    return Result.Fail<CompletedRentalDTO>(
                        ErrorResults.UnauthorizedError("Não é permitido utilizar cupons de outra empresa."));

                if (coupon.ExpirationDate < command.ActualReturnDate)
                    return Result.Fail<CompletedRentalDTO>(
                        ErrorResults.InvalidRequestError("O cupom informado está expirado para a data de devolução."));

                if (coupon.HasAlreadyBeenUsedBy(rental.ClientId))
                    return Result.Fail<CompletedRentalDTO>(
                        ErrorResults.InvalidRequestError("Este cliente já utilizou este cupom em um aluguel anterior."));

                bool markedAsUsed = coupon.TryMarkAsUsedBy(rental.ClientId);

                if (!markedAsUsed)
                    return Result.Fail<CompletedRentalDTO>(
                        ErrorResults.InvalidRequestError("Não foi possível aplicar o cupom, ele já foi utilizado anteriormente por este cliente."));

                couponId = coupon.Id;
                couponDiscountAmount = coupon.Value;
            }

            // 8) Aplica os valores na entidade (regra de caução, dano, desconto, etc.)
            rental.CompleteReturn(
                command.ActualReturnDate,
                command.InitialOdometerInKm,
                command.CurrentOdometerInKm,
                command.IsFuelTankFullOnReturn,
                command.HasDamage,
                rentalBasePrice,
                insuranceTotalPrice,
                servicesTotalPrice,
                fuelChargePrice,
                penaltyPrice,
                grossRentalAmount,
                couponId,
                couponDiscountAmount);

            await unitOfWork.CommitAsync();

            CompletedRentalDTO completedRentalDto = new(
                CompletedSuccessfully: true,
                RentalId: rental.Id,
                GrossRentalAmount: rental.GrossRentalAmount,
                FinalAmountToPay: rental.FinalAmountToPay,
                CouponId: couponId,
                CouponDiscountAmount: couponDiscountAmount);

            return Result.Ok(completedRentalDto);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackAsync();

            logger.LogError(
                exception,
                "Ocorreu um erro durante a devolução de aluguel {@Command}.",
                command);

            return Result.Fail(
                ErrorResults.InternalExceptionError(exception));
        }
    }

    private async Task<Result<IReadOnlyCollection<Service>>> LoadSelectedServicesAsync(
        IReadOnlyCollection<Guid> serviceIds,
        Guid companyId)
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