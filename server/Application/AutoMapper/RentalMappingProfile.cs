using AutoMapper;
using OblivionDrive.Application.RentalModule.DTOs;
using OblivionDrive.Domain.RentalModule;

namespace OblivionDrive.Application.AutoMapper;
public class RentalMappingProfile : Profile
{
    public RentalMappingProfile()
    {
        CreateMap<Rental, RentalDTO>()
            .ConstructUsing(rental => new RentalDTO(
                true,
                rental.Id,
                rental.EstimatedRentalAmount));

        CreateMap<Rental, UpdatedRentalDTO>()
            .ConstructUsing(rental => new UpdatedRentalDTO(
                true,
                rental.Id,
                rental.EstimatedRentalAmount));

        CreateMap<Rental, CompletedRentalDTO>()
            .ConstructUsing(rental => new CompletedRentalDTO(
                true,
                rental.Id,
                rental.GrossRentalAmount,
                rental.FinalAmountToPay,
                rental.CouponId,
                rental.CouponDiscountAmount));

        CreateMap<Rental, DetailRentalDTO>()
            .ConstructUsing(rental => new DetailRentalDTO(
                rental.Id,
                rental.ClientId,
                rental.DriverId,
                rental.VehicleId,
                rental.PlanType,
                rental.StartDate,
                rental.ExpectedReturnDate,
                rental.ActualReturnDate,
                rental.EstimatedRentalAmount,
                rental.GrossRentalAmount,
                rental.FinalAmountToPay,
                rental.IsCompleted,
                rental.CouponId,
                rental.ServiceIds));
    }
}
