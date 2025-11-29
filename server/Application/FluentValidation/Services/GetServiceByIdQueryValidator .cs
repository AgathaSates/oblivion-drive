using FluentValidation;
using OblivionDrive.Application.ServicesModule.Querys;

namespace OblivionDrive.Application.FluentValidation.Services;
public sealed class GetServiceByIdQueryValidator : AbstractValidator<GetServiceByIdQuery>
{
    public GetServiceByIdQueryValidator()
    {
        RuleFor(q => q.ServiceId)
            .NotEmpty()
                .WithMessage("O identificador do serviço é obrigatório.");
    }
}