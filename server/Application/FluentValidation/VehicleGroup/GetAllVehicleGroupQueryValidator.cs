using FluentValidation;
using OblivionDrive.Application.VehicleGroupModule.Querys;

namespace OblivionDrive.Application.FluentValidation.VehicleGroup;
public  class GetAllVehicleGroupQueryValidator : AbstractValidator<GetAllVehicleGroupQuery>
{
    private const int MaximumQuantity = 1_000;

    public GetAllVehicleGroupQueryValidator()
    {
        RuleFor(q => q.Quantity)
            .GreaterThan(0)
                .When(q => q.Quantity.HasValue)
                .WithMessage("A quantidade deve ser maior que zero.")
            .LessThanOrEqualTo(MaximumQuantity)
                .When(q => q.Quantity.HasValue)
                .WithMessage($"A quantidade não pode ser maior que {MaximumQuantity}.");
    }
}
