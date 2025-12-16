using System.Globalization;
using OblivionDrive.Application.RentalModule.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OblivionDrive.Infrastructure.Orm.Pdf.RentalModule;
public class RentalReceiptPdfDocument(RentalReceiptPdfData receiptData) : IDocument
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
            column.Item().Text("Recibo de Locação (aluguel encerrado)")
                .FontSize(12)
                .FontColor(Colors.Grey.Darken2);

            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().PaddingTop(8).Element(e =>
                e.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2))
                 .Row(row =>
                 {
                     row.RelativeItem().Text($"Aluguel: {receiptData.RentalId}");
                     row.RelativeItem().AlignRight().Text($"Plano: {receiptData.PlanTypeDisplayName}");
                 })
            );

        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().Element(ComposeRentalInfoCard);
            column.Item().Element(ComposeAmountsTable);
            column.Item().AlignRight().Element(ComposeTotalsCard);
        });
    }

    private void ComposeRentalInfoCard(IContainer container)
    {
        container
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Column(column =>
            {
                column.Spacing(6);

                column.Item().Text("Dados").SemiBold();

                column.Item().Text($"Cliente: {receiptData.ClientName}");
                column.Item().Text($"Condutor: {receiptData.DriverName}");

                column.Item().PaddingTop(6).Text("Veículo").SemiBold();
                column.Item().Text($"{receiptData.VehicleBrand} {receiptData.VehicleModel}");
                column.Item().Text($"Placa: {receiptData.VehicleLicensePlate}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken2);

                column.Item().PaddingTop(6).Element(e =>
                    e.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2))
                     .Row(row =>
                     {
                         row.RelativeItem().Text($"Saída: {receiptData.StartDate:dd/MM/yyyy}");
                         row.RelativeItem().Text($"Prev. devolução: {receiptData.ExpectedReturnDate:dd/MM/yyyy}");
                         row.RelativeItem().Text($"Devolução: {receiptData.ActualReturnDate:dd/MM/yyyy}");
                     })
                );


                column.Item().PaddingTop(6).Element(e =>
                    e.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2))
                     .Row(row =>
                     {
                         row.RelativeItem().Text($"Tanque cheio na devolução: {(receiptData.IsFuelTankFullOnReturn ? "Sim" : "Não")}");
                         row.RelativeItem().Text($"Danos: {(receiptData.HasDamage ? "Sim" : "Não")}");
                     })
                );

            });
    }

    private void ComposeAmountsTable(IContainer container)
    {
        var receiptItems = BuildReceiptItems(receiptData);

        container
            .Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Column(column =>
            {
                column.Item().Text("Detalhamento").SemiBold();

                column.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(140);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Item").SemiBold();
                        header.Cell().AlignRight().Text("Valor").SemiBold();
                        header.Cell().ColumnSpan(2).PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    foreach (var item in receiptItems)
                    {
                        table.Cell().Text(item.Description);
                        table.Cell().AlignRight().Text(FormatMoney(item.Amount));
                    }

                    if (receiptData.HasDamage && receiptData.SecurityDepositAmount > 0m)
                    {
                        column.Item().PaddingTop(6)
                            .Text($"Caução retida por danos: {FormatMoney(receiptData.SecurityDepositAmount)} (não abatida do valor a pagar).")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });

                column.Item().PaddingTop(8).Text($"Valor previsto na criação: {FormatMoney(receiptData.EstimatedRentalAmount)}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken2);
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
                    row.RelativeItem().Text("Total bruto").SemiBold();
                    row.ConstantItem(140).AlignRight().Text(FormatMoney(receiptData.GrossRentalAmount)).SemiBold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("A pagar").FontSize(14).SemiBold();
                    row.ConstantItem(140).AlignRight().Text(FormatMoney(receiptData.FinalAmountToPay)).FontSize(14).SemiBold();
                });
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Element(e =>
            e.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken1))
             .Text(text =>
             {
                 text.Span("Documento ilustrativo (nota/recibo fake). ");
                 text.Span($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}.");
             })
        );
    }

    private static string FormatMoney(decimal amount)
        => amount.ToString("C", PtBrCulture);

    private static IReadOnlyList<ReceiptItem> BuildReceiptItems(RentalReceiptPdfData data)
    {
        var items = new List<ReceiptItem>
        {
            new("Plano + KM", data.RentalBasePrice),
            new("Seguro", data.InsuranceTotalPrice),
            new("Serviços", data.ServicesTotalPrice),
            new("Combustível (taxa)", data.FuelChargePrice),
            new("Multa", data.PenaltyPrice),
        };

        if (!data.HasDamage && data.SecurityDepositAmount > 0m)
            items.Add(new("Caução (abatimento)", -data.SecurityDepositAmount));

        if (data.CouponDiscountAmount > 0m)
            items.Add(new("Cupom (desconto)", -data.CouponDiscountAmount));

        return items.Where(item => item.Amount != 0m).ToList();
    }

    private sealed record ReceiptItem(string Description, decimal Amount);
}