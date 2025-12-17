using System.Globalization;
using OblivionDrive.Application.RentalModule.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OblivionDrive.Infrastructure.Orm.Pdf.RentalModule;

public class RentalPaymentsReportPdfDocument(RentalPaymentsReportPdfData reportData) : IDocument
{
    private static readonly CultureInfo PtBrCulture = CultureInfo.GetCultureInfo("pt-BR");

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("OblivionDrive").SemiBold().FontSize(18);

            column.Item().Text("Relatório de aluguéis — Resumo financeiro")
                .FontSize(12)
                .FontColor(Colors.Grey.Darken2);

            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(8)
                .DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2))
                .Row(row =>
                {
                    row.RelativeItem().Text($"Quantidade: {reportData.Rows.Count}");
                    row.RelativeItem().AlignRight().Text($"Gerado em: {reportData.GeneratedAt:dd/MM/yyyy HH:mm}");
                });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().Element(ComposeTable);
            column.Item().AlignRight().Element(ComposeTotalsCard);
        });
    }

    private void ComposeTable(IContainer container)
    {
        container
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Column(column =>
            {
                column.Item().Text("Aluguéis concluídos").SemiBold();

                if (reportData.Rows.Count == 0)
                {
                    column.Item().PaddingTop(10)
                        .Text("Nenhum aluguel concluído encontrado para este relatório.")
                        .FontColor(Colors.Grey.Darken2);
                    return;
                }

                column.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(73);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(2.6f);
                        columns.ConstantColumn(62);
                        columns.ConstantColumn(105);
                        columns.ConstantColumn(105);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Devolução").SemiBold();
                        header.Cell().Text("Cliente").SemiBold();
                        header.Cell().Text("Veículo").SemiBold();
                        header.Cell().Text("Plano").SemiBold();
                        header.Cell().AlignRight().Text("Total (bruto)").SemiBold();
                        header.Cell().AlignRight().Text("Pago na devolução").SemiBold();

                        header.Cell().ColumnSpan(6).PaddingTop(4)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    foreach (RentalPaymentsReportRow row in reportData.Rows)
                    {
                        table.Cell().PaddingRight(5).Text(FormatDate(row.ActualReturnDate));

                        table.Cell().Text(row.ClientName);

                        table.Cell()
                            .Text(row.VehicleLabel)
                            .FontSize(10)
                            .LineHeight(1.1f);

                        table.Cell().Text(row.PlanTypeLabel);

                        table.Cell()
                          .AlignRight()
                          .Text(FormatMoney(row.GrossRentalAmount))
                          .FontSize(10)
                          .LineHeight(1);

                        table.Cell()
                          .AlignRight()
                          .Text(FormatMoney(row.FinalAmountToPay))
                          .FontSize(10)
                          .LineHeight(1);

                    }
                });
            });
    }

    private void ComposeTotalsCard(IContainer container)
    {
        container
            .Width(320)
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(6);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total do aluguel (bruto)").SemiBold();
                    row.ConstantItem(140).AlignRight().Text(FormatMoney(reportData.TotalGrossAmount)).SemiBold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Descontos (cupons)").FontColor(Colors.Grey.Darken2);
                    row.ConstantItem(140).AlignRight().Text(FormatMoney(reportData.TotalCouponDiscountAmount)).FontColor(Colors.Grey.Darken2);
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total líquido (após cupons)").FontSize(14).SemiBold();
                    row.ConstantItem(140).AlignRight().Text(FormatMoney(reportData.TotalNetAmountAfterCoupons)).FontSize(14).SemiBold();
                });
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter()
            .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken1))
            .Text("Documento ilustrativo (relatório).");
    }

    private static string FormatMoney(decimal amount)
        => amount.ToString("C", PtBrCulture);

    private static string FormatDate(DateOnly? date)
        => date is null ? "-" : date.Value.ToString("dd/MM/yyyy");
}