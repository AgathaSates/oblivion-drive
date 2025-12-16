using OblivionDrive.Application.RentalModule.DTOs;

namespace OblivionDrive.Application.RentalModule.Services;
public interface IRentalReceiptPdfGenerator
{
    byte[] Generate(RentalReceiptPdfData receiptData);
}