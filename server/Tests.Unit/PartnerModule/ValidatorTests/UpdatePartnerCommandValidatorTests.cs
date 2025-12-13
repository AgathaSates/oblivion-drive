using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Partner;
using OblivionDrive.Application.PartnerModule.Commands;

namespace OblivionDrive.Tests.Unit.PartnerModule.ValidatorTests;

[TestClass]
[TestCategory("Partner - UpdatePartnerCommandValidator Unit Tests")]
public class UpdatePartnerCommandValidatorTests
{
    private UpdatePartnerCommandValidator _validator = null!;

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 200;

    private readonly string _partnerIdRequiredMessage =
        "O identificador do parceiro é obrigatório.";

    private readonly string _nameRequiredMessage =
        "O nome do parceiro é obrigatório.";

    private readonly string _nameMinMessage =
        $"O nome do parceiro deve ter pelo menos {MinimumNameLength} caracteres.";

    private readonly string _nameMaxMessage =
        $"O nome do parceiro deve ter no máximo {MaximumNameLength} caracteres.";

    private readonly string _namePatternMessage =
        "O nome do parceiro deve conter apenas letras e espaços.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdatePartnerCommandValidator();
    }

    private static UpdatePartnerCommand CreateValidCommand()
    {
        return new UpdatePartnerCommand(
            PartnerId: Guid.NewGuid(),
            Name: "Parceiro Válido"
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_PartnerId_Is_Empty()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand() with { PartnerId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdatePartnerCommand.PartnerId) &&
            error.ErrorMessage == _partnerIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Empty()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand() with { Name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdatePartnerCommand.Name) &&
            error.ErrorMessage == _nameRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand() with { Name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdatePartnerCommand.Name) &&
            error.ErrorMessage == _nameMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        string longName = new string('A', MaximumNameLength + 1);
        UpdatePartnerCommand command = CreateValidCommand() with { Name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdatePartnerCommand.Name) &&
            error.ErrorMessage == _nameMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Contains_Invalid_Characters()
    {
        // arrange
        UpdatePartnerCommand command = CreateValidCommand() with { Name = "Parceiro 123" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(UpdatePartnerCommand.Name) &&
            error.ErrorMessage == _namePatternMessage));
    }
}