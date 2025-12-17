using System.Globalization;
using System.Text;
using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OblivionDrive.Application.RentalModule.Querys;
using OblivionDrive.Application.Shared;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Application.RentalModule.Handlers;
public sealed class ExportRentalsCsvHandler(
    UserManager<User> userManager, ITenantProvider tenantProvider, IRepositoryRental rentalRepository,
    ILogger<ExportRentalsCsvHandler> logger) : IRequestHandler<ExportRentalsCsvQuery, Result<(byte[] Content, string FileName)>>
{
    public async Task<Result<(byte[] Content, string FileName)>> Handle(ExportRentalsCsvQuery query, CancellationToken cancellationToken)
    {
        if (tenantProvider.UserId is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário não está autenticado."));

        User? currentUser = await userManager.FindByIdAsync(tenantProvider.UserId.Value.ToString());
        if (currentUser is null)
            return Result.Fail(ErrorResults.UnauthorizedError("Usuário autenticado não foi encontrado."));

        Guid companyId = currentUser.CompanyId ?? currentUser.Id;

        try
        {
            List<RentalSummaryRow> rows = await rentalRepository.GetSummaryRowsByCompanyIdAsync(companyId, query.Quantity, cancellationToken);

            CultureInfo ptBrCulture = CultureInfo.GetCultureInfo("pt-BR");

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("sep=;");
            csvBuilder.AppendLine("AluguelId;Cliente;Veículo;Plano;Saída;PrevRetorno;Devolução;Status;TotalBruto;ValorFinalAPagar");

            foreach (RentalSummaryRow row in rows)
            {
                string rentalId = row.RentalId.ToString("N");
                string vehicleDisplay = $"{row.VehicleBrand} {row.VehicleModel} ({row.VehicleLicensePlate})";
                string status = row.IsCompleted ? "Concluído" : "Em aberto";

                csvBuilder
                    .Append(Escape(rentalId)).Append(';')
                    .Append(Escape(row.ClientName)).Append(';')
                    .Append(Escape(vehicleDisplay)).Append(';')
                    .Append(Escape(row.PlanType.ToString())).Append(';')
                    .Append(Escape(row.StartDate.ToString("dd/MM/yyyy"))).Append(';')
                    .Append(Escape(row.ExpectedReturnDate.ToString("dd/MM/yyyy"))).Append(';')
                    .Append(Escape(row.ActualReturnDate?.ToString("dd/MM/yyyy") ?? "")).Append(';')
                    .Append(Escape(status)).Append(';')
                    .Append(Escape(row.GrossRentalAmount.ToString("C", ptBrCulture))).Append(';')
                    .Append(Escape(row.FinalAmountToPay.ToString("C", ptBrCulture)))
                    .AppendLine();
            }

            byte[] bom = Encoding.UTF8.GetPreamble();
            byte[] content = Encoding.UTF8.GetBytes(csvBuilder.ToString());

            byte[] fileBytes = new byte[bom.Length + content.Length];
            Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
            Buffer.BlockCopy(content, 0, fileBytes, bom.Length, content.Length);

            string fileName = $"Alugueis_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return Result.Ok((fileBytes, fileName));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao exportar aluguéis CSV.");
            return Result.Fail(ErrorResults.InternalExceptionError(ex));
        }
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        bool mustQuote = value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        string escaped = value.Replace("\"", "\"\"");
        return mustQuote ? $"\"{escaped}\"" : escaped;
    }
}