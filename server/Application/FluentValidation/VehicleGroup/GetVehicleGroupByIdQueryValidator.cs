using FluentValidation;
using OblivionDrive.Application.VehicleGroupModule.Querys;

namespace OblivionDrive.Application.FluentValidation.VehicleGroup;
public class GetVehicleGroupByIdQueryValidator: AbstractValidator<GetVehicleGroupByIdQuery>
{
    public GetVehicleGroupByIdQueryValidator()
    {
        RuleFor(q => q.VehicleGroupId)
            .NotEmpty()
                .WithMessage("O identificador do grupo de veículos é obrigatório.");
    }
}