using FluentValidation;
using OblivionDrive.Application.ServicesModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Services;
public sealed class DeleteServiceCommandValidator : AbstractValidator<DeleteServiceCommand>
{
    public DeleteServiceCommandValidator()
    {
        RuleFor(c => c.ServiceId)
            .NotEmpty()
                .WithMessage("O identificador do serviço é obrigatório.");
    }
}