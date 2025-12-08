using FluentValidation;
using OblivionDrive.Application.RentalModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Rental;

public class GetAllRentalsQueryValidator : AbstractValidator<GetAllRentalsQuery>
{
    public GetAllRentalsQueryValidator()
    {
        RuleFor(q => q.Quantity)
            .GreaterThan(0)
            .When(q => q.Quantity.HasValue)
            .WithMessage("A quantidade deve ser maior que zero.");
    }
}