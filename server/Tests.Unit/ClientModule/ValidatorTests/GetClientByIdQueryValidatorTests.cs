using FluentValidation.Results;
using OblivionDrive.Application.ClientModule.Querys;
using OblivionDrive.Application.FluentValidation.Client;

namespace OblivionDrive.Tests.Unit.ClientModule.ValidatorTests;

[TestClass]
[TestCategory("Client - GetClientByIdQueryValidator Unit Tests")]
public class GetClientByIdQueryValidatorTests
{
    private GetClientByIdQueryValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new GetClientByIdQueryValidator();
    }

    private static GetClientByIdQuery CreateValidQuery()
    {
        return new GetClientByIdQuery(
            ClientId: Guid.NewGuid()
        );
    }

    [TestMethod]
    public void Should_Pass_When_Query_Is_Valid()
    {
        // arrange
        GetClientByIdQuery query = CreateValidQuery();

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Should_Fail_When_ClientId_Is_Empty()
    {
        // arrange
        GetClientByIdQuery query = CreateValidQuery() with { ClientId = Guid.Empty };

        // act
        ValidationResult result = _validator.Validate(query);

        // assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e =>
            e.PropertyName == nameof(GetClientByIdQuery.ClientId) &&
            e.ErrorMessage == "O identificador do cliente é obrigatório."));
    }
}
