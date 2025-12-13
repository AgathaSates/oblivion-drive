using FluentValidation.Results;
using OblivionDrive.Application.DriverModule.Commands;
using OblivionDrive.Application.FluentValidation.Driver;

namespace OblivionDrive.Tests.Unit.DriverModule.ValidatorTests;

[TestClass]
[TestCategory("Driver - RegisterDriverCommandValidator Unit Tests")]
public class RegisterDriverCommandValidatorTests
{
    private RegisterDriverCommandValidator _validator = null!;

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    private const int MaximumEmailLength = 255;
    private const int MaximumPhoneLength = 20;

    private const int MaximumCpfLength = 14;
    private const int MaximumCnhLength = 20;

    private const string NameRequiredMessage =
        "O nome do condutor é obrigatório.";

    private readonly string _nameMinMessage =
        $"O nome do condutor deve ter pelo menos {MinimumNameLength} caracteres.";

    private readonly string _nameMaxMessage =
        $"O nome do condutor deve ter no máximo {MaximumNameLength} caracteres.";

    private const string NamePatternMessage =
        "O nome do condutor deve conter apenas letras e espaços.";

    private const string EmailRequiredMessage =
        "O email do condutor é obrigatório.";

    private readonly string _emailMaxMessage =
        $"O email do condutor deve ter no máximo {MaximumEmailLength} caracteres.";

    private const string EmailInvalidMessage =
        "O email do condutor deve ser válido.";

    private const string PhoneRequiredMessage =
        "O telefone do condutor é obrigatório.";

    private readonly string _phoneMaxMessage =
        $"O telefone do condutor deve ter no máximo {MaximumPhoneLength} caracteres.";

    private const string PhoneDigitsOnlyMessage =
        "O telefone do condutor deve conter apenas números.";

    private const string CpfRequiredMessage =
        "O CPF do condutor é obrigatório.";

    private readonly string _cpfMaxMessage =
        $"O CPF do condutor deve ter no máximo {MaximumCpfLength} caracteres.";

    private const string CpfPatternMessage =
        "O CPF do condutor deve conter exatamente 11 dígitos numéricos.";

    private const string CnhRequiredMessage =
        "A CNH do condutor é obrigatória.";

    private readonly string _cnhMaxMessage =
        $"A CNH do condutor deve ter no máximo {MaximumCnhLength} caracteres.";

    private const string CnhDigitsOnlyMessage =
        "A CNH do condutor deve conter apenas números.";

    private const string CnhExpirationDateMessage =
        "A validade da CNH do condutor deve ser maior ou igual à data atual.";

    private const string ClientIdRequiredMessage =
        "O identificador do cliente vinculado ao condutor é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new RegisterDriverCommandValidator();
    }

    private static RegisterDriverCommand CreateValidCommand()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        return new RegisterDriverCommand(
            Name: "Joao da Silva",
            Email: "joao.silva@email.com",
            PhoneNumber: "47999999999",
            Cpf: "12345678901",
            Cnh: "1234567890",
            CnhExpirationDate: today,
            ClientId: Guid.NewGuid(),
            IsClientAlsoDriver: false
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Empty()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Name) &&
            error.ErrorMessage == NameRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Name) &&
            error.ErrorMessage == _nameMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        string longName = new string('A', MaximumNameLength + 1);
        RegisterDriverCommand command = CreateValidCommand() with { Name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Name) &&
            error.ErrorMessage == _nameMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Has_Invalid_Characters()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Name = "Joao 123" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Name) &&
            error.ErrorMessage == NamePatternMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Empty()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Email = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Email) &&
            error.ErrorMessage == EmailRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Too_Long()
    {
        // arrange
        string tooLongEmail = new string('a', MaximumEmailLength + 1);
        RegisterDriverCommand command = CreateValidCommand() with { Email = tooLongEmail };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Email) &&
            error.ErrorMessage == _emailMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Email_Is_Invalid()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Email = "email-invalido" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Email) &&
            error.ErrorMessage == EmailInvalidMessage));
    }

    [TestMethod]
    public void Should_Fail_When_PhoneNumber_Is_Empty()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { PhoneNumber = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.PhoneNumber) &&
            error.ErrorMessage == PhoneRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_PhoneNumber_Is_Too_Long()
    {
        // arrange
        string tooLongPhone = new string('1', MaximumPhoneLength + 1);
        RegisterDriverCommand command = CreateValidCommand() with { PhoneNumber = tooLongPhone };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.PhoneNumber) &&
            error.ErrorMessage == _phoneMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_PhoneNumber_Has_Non_Digits()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { PhoneNumber = "47-99999-9999" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.PhoneNumber) &&
            error.ErrorMessage == PhoneDigitsOnlyMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Cpf_Is_Empty()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Cpf = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Cpf) &&
            error.ErrorMessage == CpfRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Cpf_Is_Too_Long()
    {
        // arrange
        string tooLongCpf = new string('1', MaximumCpfLength + 1);
        RegisterDriverCommand command = CreateValidCommand() with { Cpf = tooLongCpf };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Cpf) &&
            error.ErrorMessage == _cpfMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Cpf_Does_Not_Have_Exactly_11_Digits()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Cpf = "1234567890" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Cpf) &&
            error.ErrorMessage == CpfPatternMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Cnh_Is_Empty()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Cnh = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Cnh) &&
            error.ErrorMessage == CnhRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Cnh_Is_Too_Long()
    {
        // arrange
        string tooLongCnh = new string('1', MaximumCnhLength + 1);
        RegisterDriverCommand command = CreateValidCommand() with { Cnh = tooLongCnh };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Cnh) &&
            error.ErrorMessage == _cnhMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Cnh_Has_Non_Digits()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { Cnh = "ABC123" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.Cnh) &&
            error.ErrorMessage == CnhDigitsOnlyMessage));
    }

    [TestMethod]
    public void Should_Fail_When_CnhExpirationDate_Is_In_The_Past()
    {
        // arrange
        DateOnly yesterday = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        RegisterDriverCommand command = CreateValidCommand() with { CnhExpirationDate = yesterday };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.CnhExpirationDate) &&
            error.ErrorMessage == CnhExpirationDateMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ClientId_Is_Empty()
    {
        // arrange
        RegisterDriverCommand command = CreateValidCommand() with { ClientId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(RegisterDriverCommand.ClientId) &&
            error.ErrorMessage == ClientIdRequiredMessage));
    }
}