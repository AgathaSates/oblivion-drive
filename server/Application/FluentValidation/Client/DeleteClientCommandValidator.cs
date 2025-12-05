using FluentValidation;
using OblivionDrive.Application.ClientModule.Commands;

namespace OblivionDrive.Application.FluentValidation.Client;
public class DeleteClientCommandValidator : AbstractValidator<DeleteClientCommand>
{
    public DeleteClientCommandValidator()
    {
        RuleFor(c => c.ClientId)
            .NotEmpty()
                .WithMessage("O identificador do cliente é obrigatório.");
    }
}