using FluentValidation.Results;
using OblivionDrive.Application.BillingPlanModule.Commands;
using OblivionDrive.Application.FluentValidation.BillingPlan;

namespace OblivionDrive.Tests.Unit.BillingPlanModule.ValidatorTests;

[TestClass]
[TestCategory("BillingPlan - DeleteBillingPlanCommandValidator Unit Tests")]
public class DeleteBillingPlanCommandValidatorTests
{
    private DeleteBillingPlanCommandValidator _validator = null!;

    private readonly string _billingPlanIdRequiredMessage =
        "O identificador do plano de cobrança é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteBillingPlanCommandValidator();
    }

    private static DeleteBillingPlanCommand CreateValidCommand()
    {
        return new DeleteBillingPlanCommand(
            BillingPlanId: Guid.NewGuid());
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        DeleteBillingPlanCommand command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_BillingPlanId_Is_Empty()
    {
        // arrange
        DeleteBillingPlanCommand command = new DeleteBillingPlanCommand(
            BillingPlanId: Guid.Empty);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(DeleteBillingPlanCommand.BillingPlanId) &&
            e.ErrorMessage == _billingPlanIdRequiredMessage));
    }
}