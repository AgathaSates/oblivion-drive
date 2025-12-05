using FluentValidation;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Application.FluentValidation.Client;
public class RegisterClientCommandValidator : AbstractValidator<RegisterClientCommand>
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    private const int MaximumEmailLength = 255;
    private const int MaximumPhoneLength = 20;

    private const int MaximumCpfLength = 14;
    private const int MaximumRgLength = 20;
    private const int MaximumCnhLength = 20;
    private const int MaximumCnpjLength = 18;

    private const int MaximumStateLength = 100;
    private const int MaximumCityLength = 150;
    private const int MaximumDistrictLength = 150;
    private const int MaximumStreetLength = 200;
    private const int MaximumNumberLength = 20;

    private const string NamePattern = @"^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$";
    private const string DigitsOnlyPattern = @"^[0-9]+$";
    private const string CpfPattern = @"^[0-9]{11}$";
    private const string CnpjPattern = @"^[0-9]{14}$";

    public RegisterClientCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
                .WithMessage("O nome do cliente é obrigatório.")
            .MinimumLength(MinimumNameLength)
                .WithMessage($"O nome do cliente deve ter pelo menos {MinimumNameLength} caracteres.")
            .MaximumLength(MaximumNameLength)
                .WithMessage($"O nome do cliente deve ter no máximo {MaximumNameLength} caracteres.")
            .Matches(NamePattern)
                .WithMessage("O nome do cliente deve conter apenas letras e espaços.");

        RuleFor(c => c.Email)
            .NotEmpty()
                .WithMessage("O email do cliente é obrigatório.")
            .MaximumLength(MaximumEmailLength)
                .WithMessage($"O email do cliente deve ter no máximo {MaximumEmailLength} caracteres.")
            .EmailAddress()
                .WithMessage("O email do cliente deve ser válido.");

        RuleFor(c => c.PhoneNumber)
            .NotEmpty()
                .WithMessage("O telefone do cliente é obrigatório.")
            .MaximumLength(MaximumPhoneLength)
                .WithMessage($"O telefone do cliente deve ter no máximo {MaximumPhoneLength} caracteres.")
            .Matches(DigitsOnlyPattern)
                .WithMessage("O telefone do cliente deve conter apenas números.");

        RuleFor(c => c.ClientType)
            .IsInEnum()
                .WithMessage("O tipo de cliente é obrigatório.");

        // Pessoa Física
        When(c => c.ClientType == ClientType.Individual, () =>
        {
            RuleFor(c => c.Cpf)
                .NotEmpty()
                    .WithMessage("O CPF do cliente é obrigatório para clientes do tipo Pessoa Física.")
                .MaximumLength(MaximumCpfLength)
                    .WithMessage($"O CPF do cliente deve ter no máximo {MaximumCpfLength} caracteres.")
                .Matches(CpfPattern)
                    .WithMessage("O CPF do cliente deve conter exatamente 11 dígitos numéricos.");

            RuleFor(c => c.Rg)
                .NotEmpty()
                    .WithMessage("O RG do cliente é obrigatório para clientes do tipo Pessoa Física.")
                .MaximumLength(MaximumRgLength)
                    .WithMessage($"O RG do cliente deve ter no máximo {MaximumRgLength} caracteres.")
                .Matches(DigitsOnlyPattern)
                    .WithMessage("O RG do cliente deve conter apenas números.");

            RuleFor(c => c.Cnh)
                .NotEmpty()
                    .WithMessage("A CNH do cliente é obrigatória para clientes do tipo Pessoa Física.")
                .MaximumLength(MaximumCnhLength)
                    .WithMessage($"A CNH do cliente deve ter no máximo {MaximumCnhLength} caracteres.")
                .Matches(DigitsOnlyPattern)
                    .WithMessage("A CNH do cliente deve conter apenas números.");
        });

        // Pessoa Jurídica
        When(c => c.ClientType == ClientType.LegalEntity, () =>
        {
            RuleFor(c => c.Cnpj)
                .NotEmpty()
                    .WithMessage("O CNPJ do cliente é obrigatório para clientes do tipo Pessoa Jurídica.")
                .MaximumLength(MaximumCnpjLength)
                    .WithMessage($"O CNPJ do cliente deve ter no máximo {MaximumCnpjLength} caracteres.")
                .Matches(CnpjPattern)
                    .WithMessage("O CNPJ do cliente deve conter exatamente 14 dígitos numéricos.");
        });

        RuleFor(c => c.State)
            .NotEmpty()
                .WithMessage("O estado do endereço é obrigatório.")
            .MaximumLength(MaximumStateLength)
                .WithMessage($"O estado deve ter no máximo {MaximumStateLength} caracteres.");

        RuleFor(c => c.City)
            .NotEmpty()
                .WithMessage("A cidade do endereço é obrigatória.")
            .MaximumLength(MaximumCityLength)
                .WithMessage($"A cidade deve ter no máximo {MaximumCityLength} caracteres.");

        RuleFor(c => c.District)
            .NotEmpty()
                .WithMessage("O bairro do endereço é obrigatório.")
            .MaximumLength(MaximumDistrictLength)
                .WithMessage($"O bairro deve ter no máximo {MaximumDistrictLength} caracteres.");

        RuleFor(c => c.Street)
            .NotEmpty()
                .WithMessage("A rua do endereço é obrigatória.")
            .MaximumLength(MaximumStreetLength)
                .WithMessage($"A rua deve ter no máximo {MaximumStreetLength} caracteres.");

        RuleFor(c => c.Number)
            .NotEmpty()
                .WithMessage("O número do endereço é obrigatório.")
            .MaximumLength(MaximumNumberLength)
                .WithMessage($"O número deve ter no máximo {MaximumNumberLength} caracteres.");
    }
}