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
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Application.RentalModule.Handlers;
public sealed class GenerateRentalPaymentsReportPdfHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryRental rentalRepository,
    IRentalPaymentsReportPdfGenerator reportPdfGenerator, ILogger<GenerateRentalPaymentsReportPdfHandler> logger
) : IRequestHandler<GenerateRentalPaymentsReportPdfQuery, Result<PdfFileResult>>
{
    public async Task<Result<PdfFileResult>> Handle(GenerateRentalPaymentsReportPdfQuery query, CancellationToken cancellationToken)
    {
        if (tenantProvider.UserId is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        User? currentUser = await userManager.FindByIdAsync(tenantProvider.UserId.Value.ToString());
        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid currentCompanyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            List<RentalSummaryRow> summaryRows =
                await rentalRepository.GetSummaryRowsByCompanyIdAsync(currentCompanyId, query.Quantity, cancellationToken);

            List<RentalPaymentsReportRow> completedRows = summaryRows
                .Where(r => r.IsCompleted)
                .OrderByDescending(r => r.ActualReturnDate)
                .Select(r => new RentalPaymentsReportRow(
                    RentalId: r.RentalId,
                    ClientName: r.ClientName,
                    VehicleLabel: $"{r.VehicleBrand} {r.VehicleModel} ({r.VehicleLicensePlate})",
                    PlanTypeLabel: GetPlanTypeLabel(r.PlanType),
                    StartDate: r.StartDate,
                    ActualReturnDate: r.ActualReturnDate,
                    GrossRentalAmount: r.GrossRentalAmount,
                    FinalAmountToPay: r.FinalAmountToPay
                ))
                .ToList();

            decimal totalGrossAmount = completedRows.Sum(r => r.GrossRentalAmount);
            decimal totalFinalAmountToPay = completedRows.Sum(r => r.FinalAmountToPay);

            var reportData = new RentalPaymentsReportPdfData(
                GeneratedAt: DateTime.Now,
                Rows: completedRows,
                TotalGrossAmount: totalGrossAmount,
                TotalFinalAmountToPay: totalFinalAmountToPay
            );

            byte[] pdfBytes = reportPdfGenerator.Generate(reportData);

            string fileName = $"Relatorio_Alugueis_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return Result.Ok(new PdfFileResult(pdfBytes, fileName));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro ao gerar relatório PDF de aluguéis. Quantity={Quantity}", query.Quantity);
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