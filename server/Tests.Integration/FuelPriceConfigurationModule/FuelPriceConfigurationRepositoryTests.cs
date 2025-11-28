using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Infrastructure.Orm.FuelPriceConfigurationModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.FuelPriceConfigurationModule;

[TestClass]
[TestCategory("FuelPriceConfigurationOrmRepository Infrastructure - Integration Tests")]
public sealed class FuelPriceConfigurationOrmRepositoryTests : TestFixture
{
    [TestMethod]
    public async Task GetAsync_Should_Create_New_Configuration_When_Not_Exists()
    {
        // arrange
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        var repository = new FuelPriceConfigurationOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();

        int initialCount = await dbContext.Set<FuelPriceConfiguration>().CountAsync();

        // act
        FuelPriceConfiguration result = await repository.GetAsync(companyId);

        // assert
        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(companyId, result.CompanyId);
        Assert.AreEqual(0m, result.Gasoline);
        Assert.AreEqual(0m, result.Gas);
        Assert.AreEqual(0m, result.Diesel);
        Assert.AreEqual(0m, result.Alcohol);

        int finalCount = await dbContext.Set<FuelPriceConfiguration>().CountAsync();
        Assert.AreEqual(initialCount + 1, finalCount);

        FuelPriceConfiguration? fromDb =
            await dbContext.Set<FuelPriceConfiguration>()
                .SingleOrDefaultAsync(c => c.CompanyId == companyId);

        Assert.IsNotNull(fromDb);
        Assert.AreEqual(result.Id, fromDb!.Id);
        Assert.AreEqual(0m, fromDb.Gasoline);
        Assert.AreEqual(0m, fromDb.Gas);
        Assert.AreEqual(0m, fromDb.Diesel);
        Assert.AreEqual(0m, fromDb.Alcohol);
    }

    [TestMethod]
    public async Task GetAsync_Should_Return_Existing_Configuration_When_Already_Exists()
    {
        // arrange
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        var repository = new FuelPriceConfigurationOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();

        var existingConfiguration = new FuelPriceConfiguration(
            gasoline: 5.79m,
            gas: 4.10m,
            diesel: 6.20m,
            alcohol: 3.99m,
            companyId: companyId);

        dbContext.Set<FuelPriceConfiguration>().Add(existingConfiguration);
        await dbContext.SaveChangesAsync();

        int initialCount = await dbContext.Set<FuelPriceConfiguration>().CountAsync();

        // act
        FuelPriceConfiguration result = await repository.GetAsync(companyId);

        // assert
        Assert.IsNotNull(result);
        Assert.AreEqual(existingConfiguration.Id, result.Id);
        Assert.AreEqual(existingConfiguration.CompanyId, result.CompanyId);
        Assert.AreEqual(existingConfiguration.Gasoline, result.Gasoline);
        Assert.AreEqual(existingConfiguration.Gas, result.Gas);
        Assert.AreEqual(existingConfiguration.Diesel, result.Diesel);
        Assert.AreEqual(existingConfiguration.Alcohol, result.Alcohol);

        int finalCount = await dbContext.Set<FuelPriceConfiguration>().CountAsync();
        Assert.AreEqual(initialCount, finalCount);
    }

    [TestMethod]
    public async Task SaveAsync_Should_Update_Existing_Configuration_And_Persist_In_Database()
    {
        // arrange
        var dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        var repository = new FuelPriceConfigurationOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();

        var existingConfiguration = new FuelPriceConfiguration(
            gasoline: 1.00m,
            gas: 2.00m,
            diesel: 3.00m,
            alcohol: 4.00m,
            companyId: companyId);

        dbContext.Set<FuelPriceConfiguration>().Add(existingConfiguration);
        await dbContext.SaveChangesAsync();

        var updatedFakeConfiguration = new FuelPriceConfiguration(
            gasoline: 10.00m,
            gas: 20.00m,
            diesel: 30.00m,
            alcohol: 40.00m,
            companyId: companyId);

        // act
        await repository.SaveAsync(updatedFakeConfiguration, companyId);
        await dbContext.SaveChangesAsync();

        // assert
        FuelPriceConfiguration? fromDb =
            await dbContext.Set<FuelPriceConfiguration>()
                .SingleOrDefaultAsync(c => c.CompanyId == companyId);

        Assert.IsNotNull(fromDb);

        Assert.AreEqual(existingConfiguration.Id, fromDb!.Id);
        Assert.AreEqual(companyId, fromDb.CompanyId);

        Assert.AreEqual(updatedFakeConfiguration.Gasoline, fromDb.Gasoline);
        Assert.AreEqual(updatedFakeConfiguration.Gas, fromDb.Gas);
        Assert.AreEqual(updatedFakeConfiguration.Diesel, fromDb.Diesel);
        Assert.AreEqual(updatedFakeConfiguration.Alcohol, fromDb.Alcohol);
    }
}
