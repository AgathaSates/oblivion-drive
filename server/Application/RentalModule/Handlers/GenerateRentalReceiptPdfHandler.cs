using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Querys;
using OblivionDrive.Application.RentalModule.Results;
using OblivionDrive.Application.RentalModule.Services;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Application.RentalModule.Handlers;

public sealed class GenerateRentalReceiptPdfHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryRental rentalRepository,
    IRepositoryClient clientRepository, IRepositoryDriver driverRepository, IRepositoryVehicle vehicleRepository,
    IRentalReceiptPdfGenerator receiptPdfGenerator, ILogger<GenerateRentalReceiptPdfHandler> logger
) : IRequestHandler<GenerateRentalReceiptPdfQuery, Result<PdfFileResult>>
{
    public async Task<Result<PdfFileResult>> Handle(GenerateRentalReceiptPdfQuery query, CancellationToken cancellationToken)
    {
        if (tenantProvider.UserId is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        Guid currentUserId = tenantProvider.UserId.Value;

        User? currentUser = await userManager.FindByIdAsync(currentUserId.ToString());

        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Rental? rental = await rentalRepository.GetByIdAsync(query.RentalId);

            if (rental is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(query.RentalId));

            if (rental.CompanyId != currentCompanyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido gerar recibo de aluguel de outra empresa."));

            if (!rental.IsCompleted || rental.ActualReturnDate is null)
                return Result.Fail(ErrorResults.InvalidRequestError("O recibo só pode ser emitido para aluguéis concluídos."));

            Client? client = await clientRepository.GetByIdAsync(rental.ClientId);
            if (client is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(rental.ClientId));

            Driver? driver = await driverRepository.GetByIdAsync(rental.DriverId);
            if (driver is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(rental.DriverId));

            Vehicle? vehicle = await vehicleRepository.GetByIdAsync(rental.VehicleId);
            if (vehicle is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(rental.VehicleId));

            if (client.CompanyId != currentCompanyId ||
                driver.CompanyId != currentCompanyId ||
                vehicle.CompanyId != currentCompanyId)
            {
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido emitir recibo com dados de outra empresa."));
            }

            string planTypeDisplayName = GetPlanTypeLabel(rental.PlanType);

            var receiptData = new RentalReceiptPdfData(
                RentalId: rental.Id,

                ClientName: client.Name,
                DriverName: driver.Name,

                VehicleBrand: vehicle.Brand,
                VehicleModel: vehicle.Model,
                VehicleLicensePlate: vehicle.LicensePlate,

                PlanTypeDisplayName: planTypeDisplayName,

                StartDate: rental.StartDate,
                ExpectedReturnDate: rental.ExpectedReturnDate,
                ActualReturnDate: rental.ActualReturnDate.Value,

                HasDamage: rental.HasDamage,
                IsFuelTankFullOnReturn: rental.IsFuelTankFullOnReturn,

                RentalBasePrice: rental.RentalBasePrice,
                InsuranceTotalPrice: rental.InsuranceTotalPrice,
                ServicesTotalPrice: rental.ServicesTotalPrice,

                FuelChargePrice: rental.FuelChargePrice,
                PenaltyPrice: rental.PenaltyPrice,

                SecurityDepositAmount: rental.SecurityDepositAmount,
                CouponDiscountAmount: rental.CouponDiscountAmount,

                EstimatedRentalAmount: rental.EstimatedRentalAmount,
                GrossRentalAmount: rental.GrossRentalAmount,
                FinalAmountToPay: rental.FinalAmountToPay
            );

            byte[] pdfBytes = receiptPdfGenerator.Generate(receiptData);

            string fileName = $"Recibo_Aluguel_{rental.Id:N}.pdf";

            return Result.Ok(new PdfFileResult(pdfBytes, fileName));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ocorreu um erro ao gerar recibo PDF do aluguel {RentalId}.",
                query.RentalId);

            return Result.Fail(ErrorResults.InternalExceptionError(exception));
        }
    }
    private static string GetPlanTypeLabel(RentalPlanType planType)
    {
        return planType switch
        {
            RentalPlanType.Daily => "Diário",
            RentalPlanType.Controlled => "Controlado",
            RentalPlanType.Free => "Livre",
            _ => planType.ToString()
        };
    }
}