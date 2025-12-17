using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Services;
using QuestPDF.Fluent;

namespace OblivionDrive.Infrastructure.Orm.Pdf.RentalModule;
public class QuestPdfRentalPaymentsReportPdfGenerator : IRentalPaymentsReportPdfGenerator
{
    public byte[] Generate(RentalPaymentsReportPdfData reportData)
    {
        var document = new RentalPaymentsReportPdfDocument(reportData);

        using var pdfStream = new MemoryStream();
        document.GeneratePdf(pdfStream);

        return pdfStream.ToArray();
    }
}