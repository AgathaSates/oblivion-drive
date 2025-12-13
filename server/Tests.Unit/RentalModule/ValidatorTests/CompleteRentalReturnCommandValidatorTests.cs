using FluentValidation.Results;
using OblivionDrive.Application.FluentValidation.Rental;
using OblivionDrive.Application.RentalModule.Commands;

namespace OblivionDrive.Tests.Unit.RentalModule.ValidatorTests;

[TestClass]
[TestCategory("Rental - CompleteRentalReturnCommandValidator Unit Tests")]
public class CompleteRentalReturnCommandValidatorTests
{
    private CompleteRentalReturnCommandValidator _validator = null!;

    private static readonly DateOnly MinimumDate = new(2000, 1, 1);

    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 100;

    private const string RentalIdRequiredMessage =
        "O identificador do aluguel é obrigatório.";

    private readonly string _actualReturnDateMinMessage =
        $"A data de devolução não pode ser anterior a {MinimumDate:dd/MM/yyyy}.";

    private const string InitialOdometerNonNegativeMessage =
        "A quilometragem inicial não pode ser negativa.";

    private const string CurrentOdometerNonNegativeMessage =
        "A quilometragem atual não pode ser negativa.";

    private const string CurrentOdometerGteInitialMessage =
        "A quilometragem atual deve ser maior ou igual à quilometragem inicial.";

    private readonly string _couponMinMessage =
        $"O nome do cupom deve ter pelo menos {MinimumNameLength} caracteres.";

    private readonly string _couponMaxMessage =
        $"O nome do cupom deve ter no máximo {MaximumNameLength} caracteres.";

    private const string CouponPatternMessage =
        "O nome do cupom deve conter apenas letras maiúsculas e números, sem espaços.";

    [TestInitialize]
    public void Setup()
    {
        _validator = new CompleteRentalReturnCommandValidator();
    }

    private static CompleteRentalReturnCommand CreateValidCommand(string? couponName = null)
    {
        return new CompleteRentalReturnCommand(
            RentalId: Guid.NewGuid(),
            ActualReturnDate: new DateOnly(2025, 1, 10),
            InitialOdometerInKm: 1000,
            CurrentOdometerInKm: 1100,
            IsFuelTankFullOnReturn: true,
            HasDamage: false,
            CouponName: couponName
        );
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid_And_CouponName_Is_Null()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: null);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Pass_When_Command_Is_Valid_And_CouponName_Is_Whitespace()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: "   ");

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_RentalId_Is_Empty()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand() with { RentalId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.RentalId) &&
            e.ErrorMessage == RentalIdRequiredMessage));
    }

    [TestMethod]
    public void Should_Fail_When_ActualReturnDate_Is_Before_MinimumDate()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand() with { ActualReturnDate = MinimumDate.AddDays(-1) };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.ActualReturnDate) &&
            e.ErrorMessage == _actualReturnDateMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_InitialOdometer_Is_Negative()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand() with { InitialOdometerInKm = -1 };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.InitialOdometerInKm) &&
            e.ErrorMessage == InitialOdometerNonNegativeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_CurrentOdometer_Is_Negative()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand() with { CurrentOdometerInKm = -1 };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.CurrentOdometerInKm) &&
            e.ErrorMessage == CurrentOdometerNonNegativeMessage));
    }

    [TestMethod]
    public void Should_Fail_When_CurrentOdometer_Is_Less_Than_InitialOdometer()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand() with
        {
            InitialOdometerInKm = 1000,
            CurrentOdometerInKm = 999
        };

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.CurrentOdometerInKm) &&
            e.ErrorMessage == CurrentOdometerGteInitialMessage));
    }

    [TestMethod]
    public void Should_Pass_When_CouponName_Is_Valid()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: "CUPOM10");

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_CouponName_Is_Too_Short()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: "A");

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.CouponName) &&
            e.ErrorMessage == _couponMinMessage));
    }

    [TestMethod]
    public void Should_Fail_When_CouponName_Is_Too_Long()
    {
        // arrange
        string couponNameTooLong = new string('A', MaximumNameLength + 1);
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: couponNameTooLong);

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.CouponName) &&
            e.ErrorMessage == _couponMaxMessage));
    }

    [TestMethod]
    public void Should_Fail_When_CouponName_Has_Lowercase()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: "cupom10");

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.CouponName) &&
            e.ErrorMessage == CouponPatternMessage));
    }

    [TestMethod]
    public void Should_Fail_When_CouponName_Has_Spaces()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: "CUPOM 10");

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.CouponName) &&
            e.ErrorMessage == CouponPatternMessage));
    }

    [TestMethod]
    public void Should_Fail_When_CouponName_Has_SpecialCharacters()
    {
        // arrange
        CompleteRentalReturnCommand command = CreateValidCommand(couponName: "CUPOM-10");

        // act
        ValidationResult result = _validator.Validate(command);

        // assert
        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(CompleteRentalReturnCommand.CouponName) &&
            e.ErrorMessage == CouponPatternMessage));
    }
}