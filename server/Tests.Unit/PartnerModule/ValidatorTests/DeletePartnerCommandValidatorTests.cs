using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Partner;
using OblivionDrive.Application.PartnerModule.Commands;

namespace OblivionDrive.Tests.Unit.PartnerModule.ValidatorTests;

[TestClass]
[TestCategory("Partner - DeletePartnerCommandValidator Unit Tests")]
public class DeletePartnerCommandValidatorTests
{
    private DeletePartnerCommandValidator _validator = null!;

    private const string PartnerIdRequiredMessage =
        "O identificador do parceiro é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeletePartnerCommandValidator();
    }

    private static DeletePartnerCommand CreateValidCommand()
    {
        return new DeletePartnerCommand(
            PartnerId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_PartnerId_Is_Valid()
    {
        // arrange
        DeletePartnerCommand command = CreateValidCommand();

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
        DeletePartnerCommand command = CreateValidCommand() with { PartnerId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(error =>
            error.PropertyName == nameof(DeletePartnerCommand.PartnerId) &&
            error.ErrorMessage == PartnerIdRequiredMessage));
    }
}
