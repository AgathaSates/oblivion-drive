using FluentValidation.Results;
using OblivionDrive.Application.ClientModule.Commands;
using OblivionDrive.Application.FluentValidation.Client;
using OblivionDrive.Domain.ClientModule;

namespace OblivionDrive.Tests.Unit.ClientModule.ValidatorTests;

[TestClass]
[TestCategory("Client - UpdateClientCommandValidator Unit Tests")]
public class UpdateClientCommandValidatorTests
{
    private UpdateClientCommandValidator _validator = null!;

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

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateClientCommandValidator();
    }

    private static UpdateClientCommand CreateValidIndividualCommand()
    {
        return new UpdateClientCommand(
            ClientId: Guid.NewGuid(),
            Name: "João da Silva",
            Email: "joao.silva@example.com",
            PhoneNumber: "11987654321",
            ClientType: ClientType.Individual,
            Cpf: "12345678901",
            Rg: "123456789",
            Cnh: "12345678901",
            Cnpj: null,
            State: "São Paulo",
            City: "São Paulo",
            District: "Centro",
            Street: "Rua das Flores",
            Number: "123"
        );
    }

    private static UpdateClientCommand CreateValidLegalEntityCommand()
    {
        return new UpdateClientCommand(
            ClientId: Guid.NewGuid(),
            Name: "Empresa ABC Ltda",
            Email: "contato@empresaabc.com",
            PhoneNumber: "1133334444",
            ClientType: ClientType.LegalEntity,
            Cpf: null,
            Rg: null,
            Cnh: null,
            Cnpj: "12345678000199",
            State: "Rio de Janeiro",
            City: "Rio de Janeiro",
            District: "Copacabana",
            Street: "Avenida Atlântica",
            Number: "456"
        );
    }

    // ClientId validation tests
    [TestMethod]
    public void Should_Pass_When_Individual_Command_Is_Valid()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_LegalEntity_Command_Is_Valid()
    {
        // arrange
        UpdateClientCommand command = CreateValidLegalEntityCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_ClientId_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { ClientId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.ClientId) &&
            e.ErrorMessage == "O identificador do cliente é obrigatório."));
    }

    // Name validation tests
    [TestMethod]
    public void Should_Fail_When_Name_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Name) &&
            e.ErrorMessage == "O nome do cliente é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Name) &&
            e.ErrorMessage == $"O nome do cliente deve ter pelo menos {MinimumNameLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        string longName = new('A', MaximumNameLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { Name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Name) &&
            e.ErrorMessage == $"O nome do cliente deve ter no máximo {MaximumNameLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Contains_Invalid_Characters()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Name = "João123" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Name) &&
            e.ErrorMessage == "O nome do cliente deve conter apenas letras e espaços."));
    }

    // Email validation tests
    [TestMethod]
    public void Should_Fail_When_Email_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Email = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Email) &&
            e.ErrorMessage == "O email do cliente é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Too_Long()
    {
        // arrange
        string longEmail = new string('a', MaximumEmailLength + 1) + "@test.com";
        UpdateClientCommand command = CreateValidIndividualCommand() with { Email = longEmail };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Email) &&
            e.ErrorMessage == $"O email do cliente deve ter no máximo {MaximumEmailLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Invalid()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Email = "invalid-email" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Email) &&
            e.ErrorMessage == "O email do cliente deve ser válido."));
    }

    // PhoneNumber validation tests
    [TestMethod]
    public void Should_Fail_When_PhoneNumber_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { PhoneNumber = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.PhoneNumber) &&
            e.ErrorMessage == "O telefone do cliente é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_PhoneNumber_Is_Too_Long()
    {
        // arrange
        string longPhone = new('1', MaximumPhoneLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { PhoneNumber = longPhone };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.PhoneNumber) &&
            e.ErrorMessage == $"O telefone do cliente deve ter no máximo {MaximumPhoneLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_PhoneNumber_Contains_Non_Digits()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { PhoneNumber = "11-98765-4321" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.PhoneNumber) &&
            e.ErrorMessage == "O telefone do cliente deve conter apenas números."));
    }

    // Individual Client (CPF, RG, CNH) validation tests
    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Empty_Cpf()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Cpf = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cpf) &&
            e.ErrorMessage == "O CPF do cliente é obrigatório para clientes do tipo Pessoa Física."));
    }

    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Invalid_Cpf_Length()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Cpf = "123" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cpf) &&
            e.ErrorMessage == "O CPF do cliente deve conter exatamente 11 dígitos numéricos."));
    }

    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Cpf_Too_Long()
    {
        // arrange
        string longCpf = new('1', MaximumCpfLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { Cpf = longCpf };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cpf) &&
            e.ErrorMessage == $"O CPF do cliente deve ter no máximo {MaximumCpfLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Empty_Rg()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Rg = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Rg) &&
            e.ErrorMessage == "O RG do cliente é obrigatório para clientes do tipo Pessoa Física."));
    }

    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Rg_Too_Long()
    {
        // arrange
        string longRg = new('1', MaximumRgLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { Rg = longRg };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Rg) &&
            e.ErrorMessage == $"O RG do cliente deve ter no máximo {MaximumRgLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Rg_With_Non_Digits()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Rg = "12.345.678-9" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Rg) &&
            e.ErrorMessage == "O RG do cliente deve conter apenas números."));
    }

    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Empty_Cnh()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Cnh = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cnh) &&
            e.ErrorMessage == "A CNH do cliente é obrigatória para clientes do tipo Pessoa Física."));
    }

    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Cnh_Too_Long()
    {
        // arrange
        string longCnh = new('1', MaximumCnhLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { Cnh = longCnh };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cnh) &&
            e.ErrorMessage == $"A CNH do cliente deve ter no máximo {MaximumCnhLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Individual_Client_Has_Cnh_With_Non_Digits()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Cnh = "ABC12345678" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cnh) &&
            e.ErrorMessage == "A CNH do cliente deve conter apenas números."));
    }

    // LegalEntity Client (CNPJ) validation tests
    [TestMethod]
    public void Should_Fail_When_LegalEntity_Client_Has_Empty_Cnpj()
    {
        // arrange
        UpdateClientCommand command = CreateValidLegalEntityCommand() with { Cnpj = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cnpj) &&
            e.ErrorMessage == "O CNPJ do cliente é obrigatório para clientes do tipo Pessoa Jurídica."));
    }

    [TestMethod]
    public void Should_Fail_When_LegalEntity_Client_Has_Invalid_Cnpj_Length()
    {
        // arrange
        UpdateClientCommand command = CreateValidLegalEntityCommand() with { Cnpj = "123456" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cnpj) &&
            e.ErrorMessage == "O CNPJ do cliente deve conter exatamente 14 dígitos numéricos."));
    }

    [TestMethod]
    public void Should_Fail_When_LegalEntity_Client_Has_Cnpj_Too_Long()
    {
        // arrange
        string longCnpj = new('1', MaximumCnpjLength + 1);
        UpdateClientCommand command = CreateValidLegalEntityCommand() with { Cnpj = longCnpj };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Cnpj) &&
            e.ErrorMessage == $"O CNPJ do cliente deve ter no máximo {MaximumCnpjLength} caracteres."));
    }

    // Address validation tests
    [TestMethod]
    public void Should_Fail_When_State_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { State = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.State) &&
            e.ErrorMessage == "O estado do endereço é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_State_Is_Too_Long()
    {
        // arrange
        string longState = new('A', MaximumStateLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { State = longState };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.State) &&
            e.ErrorMessage == $"O estado deve ter no máximo {MaximumStateLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_City_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { City = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.City) &&
            e.ErrorMessage == "A cidade do endereço é obrigatória."));
    }

    [TestMethod]
    public void Should_Fail_When_City_Is_Too_Long()
    {
        // arrange
        string longCity = new('A', MaximumCityLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { City = longCity };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.City) &&
            e.ErrorMessage == $"A cidade deve ter no máximo {MaximumCityLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_District_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { District = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.District) &&
            e.ErrorMessage == "O bairro do endereço é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_District_Is_Too_Long()
    {
        // arrange
        string longDistrict = new('A', MaximumDistrictLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { District = longDistrict };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.District) &&
            e.ErrorMessage == $"O bairro deve ter no máximo {MaximumDistrictLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Street_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Street = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Street) &&
            e.ErrorMessage == "A rua do endereço é obrigatória."));
    }

    [TestMethod]
    public void Should_Fail_When_Street_Is_Too_Long()
    {
        // arrange
        string longStreet = new('A', MaximumStreetLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { Street = longStreet };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Street) &&
            e.ErrorMessage == $"A rua deve ter no máximo {MaximumStreetLength} caracteres."));
    }

    [TestMethod]
    public void Should_Fail_When_Number_Is_Empty()
    {
        // arrange
        UpdateClientCommand command = CreateValidIndividualCommand() with { Number = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Number) &&
            e.ErrorMessage == "O número do endereço é obrigatório."));
    }

    [TestMethod]
    public void Should_Fail_When_Number_Is_Too_Long()
    {
        // arrange
        string longNumber = new('1', MaximumNumberLength + 1);
        UpdateClientCommand command = CreateValidIndividualCommand() with { Number = longNumber };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateClientCommand.Number) &&
            e.ErrorMessage == $"O número deve ter no máximo {MaximumNumberLength} caracteres."));
    }
}
