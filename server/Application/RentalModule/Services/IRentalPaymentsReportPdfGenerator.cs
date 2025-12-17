using OblivionDrive.Application.RentalModule.DTOs;

namespace OblivionDrive.Application.RentalModule.Services;

public interface IRentalPaymentsReportPdfGenerator
{
    byte[] Generate(RentalPaymentsReportPdfData reportData);
}