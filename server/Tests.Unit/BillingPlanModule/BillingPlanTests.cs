using OblivionDrive.Domain.BillingPlanModule;

namespace OblivionDrive.Tests.Unit.BillingPlanModule;

[TestClass]
[TestCategory("BillingPlan - Entity Unit Tests")]
public class BillingPlanTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        string name = "Plano Padrão";
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        DailyBillingPlanConfig dailyPlan = new DailyBillingPlanConfig(
            dailyRate: 100m,
            pricePerKilometer: 2.5m);

        ControlledBillingPlanConfig controlledPlan = new ControlledBillingPlanConfig(
            dailyRate: 80m,
            extraPricePerKilometer: 3.0m);

        FreeBillingPlanConfig freePlan = new FreeBillingPlanConfig(
            dailyRate: 200m);

        // act
        BillingPlan billingPlan = new BillingPlan(
            name: name,
            companyId: companyId,
            vehicleGroupId: vehicleGroupId,
            dailyPlan: dailyPlan,
            controlledPlan: controlledPlan,
            freePlan: freePlan);

        // assert
        Assert.AreNotEqual(Guid.Empty, billingPlan.Id);
        Assert.AreEqual(companyId, billingPlan.CompanyId);
        Assert.AreEqual(vehicleGroupId, billingPlan.VehicleGroupId);

        Assert.AreEqual(name, billingPlan.Name);

        Assert.AreEqual(dailyPlan.DailyRate, billingPlan.DailyPlan.DailyRate);
        Assert.AreEqual(dailyPlan.PricePerKilometer, billingPlan.DailyPlan.PricePerKilometer);

        Assert.AreEqual(controlledPlan.DailyRate, billingPlan.ControlledPlan.DailyRate);
        Assert.AreEqual(controlledPlan.ExtraPricePerKilometer, billingPlan.ControlledPlan.ExtraPricePerKilometer);

        Assert.AreEqual(freePlan.DailyRate, billingPlan.FreePlan.DailyRate);
    }

    [TestMethod]
    public void Update_Should_Update_Properties_And_Keep_Id_CompanyId_And_VehicleGroupId()
    {
        // arrange
        Guid companyId = Guid.NewGuid();
        Guid vehicleGroupId = Guid.NewGuid();

        BillingPlan originalBillingPlan = new BillingPlan(
            name: "Plano Original",
            companyId: companyId,
            vehicleGroupId: vehicleGroupId,
            dailyPlan: new DailyBillingPlanConfig(
                dailyRate: 100m,
                pricePerKilometer: 2m),
            controlledPlan: new ControlledBillingPlanConfig(
                dailyRate: 90m,
                extraPricePerKilometer: 3m),
            freePlan: new FreeBillingPlanConfig(
                dailyRate: 200m));

        Guid originalId = originalBillingPlan.Id;
        Guid originalCompanyId = originalBillingPlan.CompanyId;
        Guid originalVehicleGroupId = originalBillingPlan.VehicleGroupId;

        BillingPlan updatedBillingPlan = new BillingPlan(
            name: "Plano Atualizado",
            companyId: Guid.NewGuid(),
            vehicleGroupId: Guid.NewGuid(),
            dailyPlan: new DailyBillingPlanConfig(
                dailyRate: 150m,
                pricePerKilometer: 2.5m),
            controlledPlan: new ControlledBillingPlanConfig(
                dailyRate: 95m,
                extraPricePerKilometer: 3.5m),
            freePlan: new FreeBillingPlanConfig(
                dailyRate: 250m));

        // act
        originalBillingPlan.Update(updatedBillingPlan);

        // assert
        Assert.AreEqual(updatedBillingPlan.Name, originalBillingPlan.Name);

        Assert.AreEqual(updatedBillingPlan.DailyPlan.DailyRate, originalBillingPlan.DailyPlan.DailyRate);
        Assert.AreEqual(updatedBillingPlan.DailyPlan.PricePerKilometer, originalBillingPlan.DailyPlan.PricePerKilometer);

        Assert.AreEqual(updatedBillingPlan.ControlledPlan.DailyRate, originalBillingPlan.ControlledPlan.DailyRate);
        Assert.AreEqual(updatedBillingPlan.ControlledPlan.ExtraPricePerKilometer, originalBillingPlan.ControlledPlan.ExtraPricePerKilometer);

        Assert.AreEqual(updatedBillingPlan.FreePlan.DailyRate, originalBillingPlan.FreePlan.DailyRate);

        Assert.AreEqual(originalId, originalBillingPlan.Id);
        Assert.AreEqual(originalCompanyId, originalBillingPlan.CompanyId);
        Assert.AreEqual(originalVehicleGroupId, originalBillingPlan.VehicleGroupId);
    }
}

[TestClass]
[TestCategory("BillingPlan - DailyBillingPlanConfig Unit Tests")]
public class DailyBillingPlanConfigTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        decimal dailyRate = 120m;
        decimal pricePerKilometer = 3.5m;

        // act
        DailyBillingPlanConfig config = new DailyBillingPlanConfig(
            dailyRate: dailyRate,
            pricePerKilometer: pricePerKilometer);

        // assert
        Assert.AreEqual(dailyRate, config.DailyRate);
        Assert.AreEqual(pricePerKilometer, config.PricePerKilometer);
    }
}

[TestClass]
[TestCategory("BillingPlan - ControlledBillingPlanConfig Unit Tests")]
public class ControlledBillingPlanConfigTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        decimal dailyRate = 90m;
        decimal extraPricePerKilometer = 4.2m;

        // act
        ControlledBillingPlanConfig config = new ControlledBillingPlanConfig(
            dailyRate: dailyRate,
            extraPricePerKilometer: extraPricePerKilometer);

        // assert
        Assert.AreEqual(dailyRate, config.DailyRate);
        Assert.AreEqual(extraPricePerKilometer, config.ExtraPricePerKilometer);
    }
}

[TestClass]
[TestCategory("BillingPlan - FreeBillingPlanConfig Unit Tests")]
public class FreeBillingPlanConfigTests
{
    [TestMethod]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // arrange
        decimal dailyRate = 300m;

        // act
        FreeBillingPlanConfig config = new FreeBillingPlanConfig(dailyRate);

        // assert
        Assert.AreEqual(dailyRate, config.DailyRate);
    }
}