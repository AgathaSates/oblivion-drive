using OblivionDrive.Domain.FuelPriceConfigurationModule;

namespace OblivionDrive.Tests.Unit.FuelPriceConfigurationModule;

[TestClass]
[TestCategory("FuelPriceConfiguration - Entity Unit Tests")]
public  class FuelPriceConfigurationTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        decimal gasoline = 5.79m;
        decimal gas = 4.10m;
        decimal diesel = 6.20m;
        decimal alcohol = 3.99m;
        DateOnly minDate = DateOnly.FromDateTime(DateTime.Now);
        Guid companyId = Guid.NewGuid();


        // act
        FuelPriceConfiguration configuration = new FuelPriceConfiguration(
            gasoline,
            gas,
            diesel,
            alcohol,
            companyId);

        DateOnly maxDate = DateOnly.FromDateTime(DateTime.Now);

        // assert
        Assert.AreNotEqual(Guid.Empty, configuration.Id);
        Assert.AreEqual(companyId, configuration.CompanyId);

        Assert.AreEqual(gasoline, configuration.Gasoline);
        Assert.AreEqual(gas, configuration.Gas);
        Assert.AreEqual(diesel, configuration.Diesel);
        Assert.AreEqual(alcohol, configuration.Alcohol);

        Assert.IsTrue(
            configuration.LastUpdate >= minDate &&
            configuration.LastUpdate <= maxDate,
            "LastUpdate deve ser a data atual.");
    }

    [TestMethod]
    public void Update_Should_Update_FuelPrices_And_Keep_Id_And_CompanyId()
    {
        // arrange
        Guid companyId = Guid.NewGuid();

        FuelPriceConfiguration originalConfiguration = new FuelPriceConfiguration(
            gasoline: 1.00m,
            gas: 2.00m,
            diesel: 3.00m,
            alcohol: 4.00m,
            companyId: companyId);

        Guid originalId = originalConfiguration.Id;
        Guid originalCompanyId = originalConfiguration.CompanyId;

        FuelPriceConfiguration updatedConfiguration = new FuelPriceConfiguration(
            gasoline: 10.00m,
            gas: 20.00m,
            diesel: 30.00m,
            alcohol: 40.00m,
            companyId: Guid.NewGuid());

        DateOnly minDate = DateOnly.FromDateTime(DateTime.Now);

        // act
        originalConfiguration.Update(updatedConfiguration);

        DateOnly maxDate = DateOnly.FromDateTime(DateTime.Now);

        // assert
        Assert.AreEqual(updatedConfiguration.Gasoline, originalConfiguration.Gasoline);
        Assert.AreEqual(updatedConfiguration.Gas, originalConfiguration.Gas);
        Assert.AreEqual(updatedConfiguration.Diesel, originalConfiguration.Diesel);
        Assert.AreEqual(updatedConfiguration.Alcohol, originalConfiguration.Alcohol);

        Assert.AreEqual(originalId, originalConfiguration.Id);
        Assert.AreEqual(originalCompanyId, originalConfiguration.CompanyId);

        Assert.IsTrue(
            originalConfiguration.LastUpdate >= minDate &&
            originalConfiguration.LastUpdate <= maxDate,
            "LastUpdate deve ser atualizado para a data atual.");
    }
}