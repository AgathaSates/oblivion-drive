using FluentValidation.Results;
using OblivionDrive.Application.CouponModule.Commands;
using OblivionDrive.Application.FluentValidation.Coupon;

namespace OblivionDrive.Tests.Unit.CouponModule.ValidatorTests;

[TestClass]
[TestCategory("Coupon - DeleteCouponCommandValidator Unit Tests")]
public sealed class DeleteCouponCommandValidatorTests
{
    private DeleteCouponCommandValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new DeleteCouponCommandValidator();
    }

    private static DeleteCouponCommand CreateValidCommand()
    {
        return new DeleteCouponCommand(
            CouponId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Fail_When_CouponId_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { CouponId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(DeleteCouponCommand.CouponId) &&
            e.ErrorMessage == "O identificador do cupom é obrigatório."));
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid()
    {
        // arrange
        var command = CreateValidCommand();

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Have_Exactly_One_Error_When_CouponId_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { CouponId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(1, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_CouponId_Is_Valid_Guid()
    {
        // arrange
        Guid validCouponId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var command = new DeleteCouponCommand(CouponId: validCouponId);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
}
