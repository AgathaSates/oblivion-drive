using FluentValidation;
using OblivionDrive.Application.ClientModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Client;
public class GetClientByIdQueryValidator : AbstractValidator<GetClientByIdQuery>
{
    public GetClientByIdQueryValidator()
    {
        RuleFor(q => q.ClientId)
            .NotEmpty()
                .WithMessage("O identificador do cliente é obrigatório.");
    }
}