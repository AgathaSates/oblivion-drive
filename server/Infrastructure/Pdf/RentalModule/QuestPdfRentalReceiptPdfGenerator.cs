using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Application.RentalModule.Services;
using QuestPDF.Fluent;

namespace OblivionDrive.Infrastructure.Orm.Pdf.RentalModule;
public class QuestPdfRentalReceiptPdfGenerator : IRentalReceiptPdfGenerator
{
    public byte[] Generate(RentalReceiptPdfData receiptData)
    {
        var document = new RentalReceiptPdfDocument(receiptData);

        using var pdfStream = new MemoryStream();
        document.GeneratePdf(pdfStream);

        return pdfStream.ToArray();
    }
}