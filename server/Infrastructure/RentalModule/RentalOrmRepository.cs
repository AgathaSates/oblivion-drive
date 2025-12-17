using Microsoft.EntityFrameworkCore;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.RentalModule;

public class RentalOrmRepository(OblivionDriveDbContext context) : BaseRepository<Rental>(context), IRepositoryRental
{
    public async Task<bool> ExistsOpenRentalForVehicleAsync(Guid vehicleId)
    {
        return await context.Rentals
            .AnyAsync(rental =>
                rental.VehicleId == vehicleId &&
                !rental.IsCompleted);
    }

    public async Task<bool> ExistsForVehicleGroupAsync(Guid vehicleGroupId)
    {
        return await context.Rentals
            .Include(rental => rental.Vehicle)
            .AnyAsync(rental => rental.Vehicle.VehicleGroupId == vehicleGroupId);
    }

    public async Task<bool> ExistsOpenRentalForClientAsync(Guid clientId)
    {
        return await context.Rentals
             .AnyAsync(rental => rental.ClientId == clientId && !rental.IsCompleted);
    }

    public async Task<bool> ExistsOpenRentalForDriverAsync(Guid driverId)
    {
        return await context.Rentals
            .AnyAsync(rental => rental.DriverId == driverId && !rental.IsCompleted);
    }

    public async Task<bool> ExistsOpenRentalUsingServiceAsync(Guid serviceId)
    {
        return context.Rentals
            .Where(r => !r.IsCompleted)
            .AsEnumerable()
            .Any(r => r.ServiceIds.Contains(serviceId));

    }

    public Task<bool> ExistsAnyRentalForVehicleAsync(Guid vehicleId)
    {
        return context.Rentals.AnyAsync(rental => rental.VehicleId == vehicleId);
    }

    public Task<bool> ExistsAnyRentalForDriverAsync(Guid driverId)
    {
        return context.Rentals.AnyAsync(r => r.DriverId == driverId);
    }

    public async Task<List<RentalSummaryRow>> GetSummaryRowsByCompanyIdAsync(Guid companyId, int? count, CancellationToken cancellationToken)
    {
        IQueryable<Rental> query = context.Rentals
            .AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.Vehicle)
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.StartDate);

        if (count.HasValue && count.Value > 0)
            query = query.Take(count.Value);

        return await query
            .Select(r => new RentalSummaryRow(
                r.Id,
                r.Client.Name,
                r.Vehicle.Brand,
                r.Vehicle.Model,
                r.Vehicle.LicensePlate,
                r.PlanType,
                r.StartDate,
                r.ExpectedReturnDate,
                r.ActualReturnDate,
                r.IsCompleted,
                r.GrossRentalAmount,
                r.FinalAmountToPay
            ))
            .ToListAsync(cancellationToken);
    }
}