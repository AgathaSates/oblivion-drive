using System.Globalization;
using System.Text;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.RentalModule.Commands;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Services;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.VehicleModule;

namespace OblivionDrive.Application.RentalModule.Handlers;

public class SendRentalReceiptEmailHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryRental rentalRepository,
    IRepositoryClient clientRepository, IRepositoryDriver driverRepository, IRepositoryVehicle vehicleRepository,
    IRentalReceiptPdfGenerator receiptPdfGenerator, IEmailSender emailSender, IValidator<SendRentalReceiptEmailCommand> validator,
    ILogger<SendRentalReceiptEmailHandler> logger) : IRequestHandler<SendRentalReceiptEmailCommand, Result>
{
    public async Task<Result> Handle(SendRentalReceiptEmailCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result.Fail(ErrorResults.InvalidRequestError(errors));
        }

        if (tenantProvider.UserId is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        User? currentUser = await userManager.FindByIdAsync(tenantProvider.UserId.Value.ToString());
        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            Rental? rental = await rentalRepository.GetByIdAsync(command.RentalId);
            if (rental is null)
                return Result.Fail(ErrorResults.RecordNotFoundError(command.RentalId));

            if (rental.CompanyId != companyId)
                return Result.Fail(ErrorResults.UnauthorizedError("Não é permitido enviar recibo de aluguel de outra empresa."));

            if (!rental.IsCompleted || rental.ActualReturnDate is null)
                return Result.Fail(ErrorResults.InvalidRequestError("O recibo só pode ser enviado para aluguéis concluídos."));

            Client? client = await clientRepository.GetByIdAsync(rental.ClientId);
            Driver? driver = await driverRepository.GetByIdAsync(rental.DriverId);
            Vehicle? vehicle = await vehicleRepository.GetByIdAsync(rental.VehicleId);

            if (client is null) return Result.Fail(ErrorResults.RecordNotFoundError(rental.ClientId));
            if (driver is null) return Result.Fail(ErrorResults.RecordNotFoundError(rental.DriverId));
            if (vehicle is null) return Result.Fail(ErrorResults.RecordNotFoundError(rental.VehicleId));

            var receiptData = new RentalReceiptPdfData(
                rental.Id,
                client.Name,
                driver.Name,
                vehicle.Brand,
                vehicle.Model,
                vehicle.LicensePlate,
                rental.PlanType.ToString(),
                rental.StartDate,
                rental.ExpectedReturnDate,
                rental.ActualReturnDate.Value,
                rental.HasDamage,
                rental.IsFuelTankFullOnReturn,
                rental.RentalBasePrice,
                rental.InsuranceTotalPrice,
                rental.ServicesTotalPrice,
                rental.FuelChargePrice,
                rental.PenaltyPrice,
                rental.SecurityDepositAmount,
                rental.CouponDiscountAmount,
                rental.EstimatedRentalAmount,
                rental.GrossRentalAmount,
                rental.FinalAmountToPay
            );

            byte[] pdfBytes = receiptPdfGenerator.Generate(receiptData);

            CultureInfo ptBrCulture = CultureInfo.GetCultureInfo("pt-BR");

            string rentalId = rental.Id.ToString("N");
            string subject = $"Recibo do aluguel encerrado — {rentalId}";

            string clientName = client.Name;

            var emailBodyBuilder = new StringBuilder()
                .AppendLine($"Olá, {clientName}!")
                .AppendLine()
                .AppendLine("Seu aluguel foi encerrado com sucesso.")
                .AppendLine()
                .AppendLine("Em anexo, você encontra o recibo em PDF com todos os detalhes do aluguel (período, valores e eventuais ajustes como caução, cupom, combustível e/ou multa).")
                .AppendLine()
                .AppendLine("Se precisar conferir rapidamente:")
                .AppendLine("• O valor final está indicado no campo “Valor final a pagar” do recibo.");

            if (rental.FinalAmountToPay > 0m)
            {
                emailBodyBuilder.AppendLine($"• Valor final a pagar: {rental.FinalAmountToPay.ToString("C", ptBrCulture)}");
            }
            else
            {
                emailBodyBuilder.AppendLine("• Não há saldo pendente a pagar.");
            }

            emailBodyBuilder
                .AppendLine()
                .AppendLine("Caso identifique qualquer divergência, é só responder este e-mail que a gente verifica.")
                .AppendLine()
                .AppendLine("Obrigado por escolher a OblivionDrive.")
                .AppendLine()
                .AppendLine("Atenciosamente,")
                .AppendLine("OblivionDrive")
                .AppendLine("(Este e-mail foi gerado automaticamente.)");

            string body = emailBodyBuilder.ToString();

            var message = new EmailMessage(
                To: command.Email,
                Subject: subject,
                Body: body,
                Attachments: new[]
                {
                    new EmailAttachment(
                        FileName: $"Recibo_Aluguel_{rental.Id:N}.pdf",
                        ContentType: "application/pdf",
                        Content: pdfBytes
                    )
                }
            );

            await emailSender.SendAsync(message, cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar recibo por e-mail. RentalId={RentalId}", command.RentalId);
            return Result.Fail(ErrorResults.InternalExceptionError(ex));
        }
    }
}