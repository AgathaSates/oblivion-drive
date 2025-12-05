using FluentValidation;
using OblivionDrive.Application.DriverModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Driver;
public class GetDriverByIdQueryValidator : AbstractValidator<GetDriverByIdQuery>
{
    public GetDriverByIdQueryValidator()
    {
        RuleFor(q => q.DriverId)
            .NotEmpty()
                .WithMessage("O identificador do condutor é obrigatório.");
    }
}