using FluentValidation.Results;
using OblivionDrive.Application.CouponModule.Commands;
using OblivionDrive.Application.FluentValidation.Coupon;

namespace OblivionDrive.Tests.Unit.CouponModule.ValidatorTests;

[TestClass]
[TestCategory("Coupon - UpdateCouponCommandValidator Unit Tests")]
public sealed class UpdateCouponCommandValidatorTests
{
    private UpdateCouponCommandValidator _validator = null!;

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 100;
    private const decimal MaximumCouponValue = 1_000_000m;

    private readonly string _couponIdRequiredMessage = "O identificador do cupom é obrigatório.";
    private readonly string _nameRequiredMessage = "O nome do cupom é obrigatório.";
    private readonly string _nameMinMessage = $"O nome do cupom deve ter pelo menos {MinimumNameLength} caracteres.";
    private readonly string _nameMaxMessage = $"O nome do cupom deve ter no máximo {MaximumNameLength} caracteres.";
    private readonly string _namePatternMessage = "O nome do cupom deve conter apenas letras maiúsculas e números, sem espaços.";
    private readonly string _valueGreaterThanZeroMessage = "O valor do cupom deve ser maior que zero.";
    private readonly string _valueMaxMessage = $"O valor do cupom não pode ser maior que {MaximumCouponValue:N2}.";
    private readonly string _expirationDateMessage = "A data de validade do cupom deve ser maior ou igual à data atual.";
    private readonly string _partnerIdRequiredMessage = "O identificador do parceiro vinculado ao cupom é obrigatório.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new UpdateCouponCommandValidator();
    }

    private static UpdateCouponCommand CreateValidCommand()
    {
        return new UpdateCouponCommand(
            CouponId: Guid.NewGuid(),
            Name: "CUPOM20",
            Value: 100.00m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            PartnerId: Guid.NewGuid()
        );
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
    public void Should_Fail_When_CouponId_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { CouponId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.CouponId) &&
            e.ErrorMessage == _couponIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Empty()
    {
        // arrange
        var command = CreateValidCommand() with { Name = string.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Name) &&
            e.ErrorMessage == _nameRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Short()
    {
        // arrange
        var command = CreateValidCommand() with { Name = "A" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Name) &&
            e.ErrorMessage == _nameMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Is_Too_Long()
    {
        // arrange
        var longName = new string('A', MaximumNameLength + 1);
        var command = CreateValidCommand() with { Name = longName };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Name) &&
            e.ErrorMessage == _nameMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Contains_Lowercase_Letters()
    {
        // arrange
        var command = CreateValidCommand() with { Name = "Cupom20" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Name) &&
            e.ErrorMessage == _namePatternMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Contains_Spaces()
    {
        // arrange
        var command = CreateValidCommand() with { Name = "CUPOM 20" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Name) &&
            e.ErrorMessage == _namePatternMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Name_Contains_Special_Characters()
    {
        // arrange
        var command = CreateValidCommand() with { Name = "CUPOM@20" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Name) &&
            e.ErrorMessage == _namePatternMessage));
    }

    [TestMethod]
    public void Should_Pass_When_Name_Contains_Only_Uppercase_And_Numbers()
    {
        // arrange
        var command = CreateValidCommand() with { Name = "CUPOM2024" };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_Value_Is_Zero()
    {
        // arrange
        var command = CreateValidCommand() with { Value = 0m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Value) &&
            e.ErrorMessage == _valueGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Value_Is_Negative()
    {
        // arrange
        var command = CreateValidCommand() with { Value = -10m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Value) &&
            e.ErrorMessage == _valueGreaterThanZeroMessage));
    }

    [TestMethod]
    public void Should_Fail_When_Value_Is_Greater_Than_Maximum()
    {
        // arrange
        var command = CreateValidCommand() with { Value = MaximumCouponValue + 0.01m };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.Value) &&
            e.ErrorMessage == _valueMaxMessage));
    }

    [TestMethod]
    public void Should_Pass_When_Value_Is_Equal_To_Maximum()
    {
        // arrange
        var command = CreateValidCommand() with { Value = MaximumCouponValue };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_ExpirationDate_Is_In_The_Past()
    {
        // arrange
        var command = CreateValidCommand() with 
        { 
            ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) 
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.ExpirationDate) &&
            e.ErrorMessage == _expirationDateMessage));
    }

    [TestMethod]
    public void Should_Pass_When_ExpirationDate_Is_Today()
    {
        // arrange
        var command = CreateValidCommand() with 
        { 
            ExpirationDate = DateOnly.FromDateTime(DateTime.Today) 
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_ExpirationDate_Is_In_The_Future()
    {
        // arrange
        var command = CreateValidCommand() with 
        { 
            ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)) 
        };

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
        var command = CreateValidCommand() with { PartnerId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(UpdateCouponCommand.PartnerId) &&
            e.ErrorMessage == _partnerIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Pass_When_PartnerId_Is_Valid()
    {
        // arrange
        var command = CreateValidCommand() with { PartnerId = Guid.NewGuid() };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_All_Fields_Are_At_Minimum_Valid_Values()
    {
        // arrange
        var command = new UpdateCouponCommand(
            CouponId: Guid.NewGuid(),
            Name: "AB",
            Value: 0.01m,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today),
            PartnerId: Guid.NewGuid()
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_All_Fields_Are_At_Maximum_Valid_Values()
    {
        // arrange
        var maxName = new string('A', MaximumNameLength);
        var command = new UpdateCouponCommand(
            CouponId: Guid.NewGuid(),
            Name: maxName,
            Value: MaximumCouponValue,
            ExpirationDate: DateOnly.FromDateTime(DateTime.Today.AddYears(10)),
            PartnerId: Guid.NewGuid()
        );

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }
}
