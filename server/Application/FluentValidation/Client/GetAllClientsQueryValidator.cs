using FluentValidation;
using OblivionDrive.Application.ClientModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Client;
public class GetAllClientsQueryValidator : AbstractValidator<GetAllClientsQuery>
{
    private const int MaximumQuantity = 1_000;

    public GetAllClientsQueryValidator()
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