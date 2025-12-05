using FluentValidation;
using OblivionDrive.Application.VehicleModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Vehicle;
public class GetAllVehiclesQueryValidator : AbstractValidator<GetAllVehiclesQuery>
{
    private const int MaximumQuantity = 1_000;

    public GetAllVehiclesQueryValidator()
    {
        RuleFor(q => q.Quantity)
            .GreaterThan(0)
                .When(q => q.Quantity.HasValue)
                .WithMessage("A quantidade deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumQuantity)
                .When(q => q.Quantity.HasValue)
                .WithMessage($"A quantidade não pode ser maior que {MaximumQuantity}.");

        RuleFor(q => q.VehicleGroupId)
            .NotEqual(Guid.Empty)
                .When(q => q.VehicleGroupId.HasValue)
                .WithMessage("O identificador do grupo de veículos, quando informado, não pode ser vazio.");
    }
}