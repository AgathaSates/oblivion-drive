using FluentResults;
using MediatR;
using OblivionDrive.Application.RentalModule.Results;

namespace OblivionDrive.Application.RentalModule.Querys;
public sealed record GenerateRentalPaymentsReportPdfQuery(int? Quantity) : IRequest<Result<PdfFileResult>>;