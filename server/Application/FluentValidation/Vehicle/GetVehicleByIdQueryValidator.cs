using FluentValidation;
using OblivionDrive.Application.VehicleModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Vehicle;
public class GetVehicleByIdQueryValidator : AbstractValidator<GetVehicleByIdQuery>
{
    public GetVehicleByIdQueryValidator()
    {
        RuleFor(q => q.VehicleId)
            .NotEmpty()
                .WithMessage("O identificador do veículo é obrigatório.");
    }
}