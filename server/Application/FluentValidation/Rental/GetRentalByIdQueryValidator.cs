using FluentValidation;
using OblivionDrive.Application.RentalModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Rental;

public class GetRentalByIdQueryValidator : AbstractValidator<GetRentalByIdQuery>
{
    public GetRentalByIdQueryValidator()
    {
        RuleFor(q => q.RentalId)
            .NotEmpty()
                .WithMessage("O identificador do aluguel é obrigatório.");
    }
}