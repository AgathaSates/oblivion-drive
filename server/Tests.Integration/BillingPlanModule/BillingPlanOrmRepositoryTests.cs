using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.BillingPlanModule;

[TestClass]
[TestCategory("BillingPlanOrmRepository Infrastructure - Integration Tests")]
public class BillingPlanOrmRepositoryTests : TestFixture
{
    private static VehicleGroup CreateVehicleGroup(Guid companyId, string name = "Grupo Teste")
    {
        return new VehicleGroup(
            name: name,
            companyId: companyId);
    }

    private static BillingPlan CreateBillingPlan(
        string name,
        Guid companyId,
        Guid vehicleGroupId)
    {
        var dailyPlan = new DailyBillingPlanConfig(
            dailyRate: 100m,
            pricePerKilometer: 1.5m);

        var controlledPlan = new ControlledBillingPlanConfig(
            dailyRate: 80m,
            extraPricePerKilometer: 2m);

        var freePlan = new FreeBillingPlanConfig(
            dailyRate: 200m);

        return new BillingPlan(
            name: name,
            companyId: companyId,
            vehicleGroupId: vehicleGroupId,
            dailyPlan: dailyPlan,
            controlledPlan: controlledPlan,
            freePlan: freePlan);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_True_When_BillingPlan_With_Same_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string billingPlanName = "Plano Padrão";

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        dbContext.VehicleGroups.Add(vehicleGroup);

        BillingPlan billingPlan = CreateBillingPlan(
            name: billingPlanName,
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        dbContext.BillingPlans.Add(billingPlan);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await billingPlanRepository.ExistsByNameAsync(billingPlanName);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_BillingPlan_With_Name_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        dbContext.VehicleGroups.Add(vehicleGroup);

        BillingPlan existingBillingPlan = CreateBillingPlan(
            name: "Plano Existente",
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        dbContext.BillingPlans.Add(existingBillingPlan);
        await dbContext.SaveChangesAsync();

        string searchedName = "Outro Plano";

        // act
        bool exists = await billingPlanRepository.ExistsByNameAsync(searchedName);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Name_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        // act
        bool existsForEmpty = await billingPlanRepository.ExistsByNameAsync(string.Empty);
        bool existsForWhitespace = await billingPlanRepository.ExistsByNameAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_False_When_Only_BillingPlan_With_Name_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string billingPlanName = "Plano Único";

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        dbContext.VehicleGroups.Add(vehicleGroup);

        BillingPlan billingPlan = CreateBillingPlan(
            name: billingPlanName,
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        dbContext.BillingPlans.Add(billingPlan);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await billingPlanRepository.ExistsByNameAsync(billingPlanName, billingPlan.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio plano como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_True_When_Other_BillingPlan_With_Same_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string billingPlanName = "Plano Duplicado";

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        dbContext.VehicleGroups.Add(vehicleGroup);

        BillingPlan billingPlan1 = CreateBillingPlan(
            name: billingPlanName,
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        BillingPlan billingPlan2 = CreateBillingPlan(
            name: billingPlanName,
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        dbContext.BillingPlans.Add(billingPlan1);
        dbContext.BillingPlans.Add(billingPlan2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await billingPlanRepository.ExistsByNameAsync(billingPlanName, billingPlan1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro plano de cobrança com o mesmo nome.");
    }

    [TestMethod]
    public async Task ExistsForVehicleGroupAsync_Should_Return_True_When_BillingPlan_For_VehicleGroup_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        dbContext.VehicleGroups.Add(vehicleGroup);

        BillingPlan billingPlan = CreateBillingPlan(
            name: "Plano para grupo",
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        dbContext.BillingPlans.Add(billingPlan);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await billingPlanRepository.ExistsForVehicleGroupAsync(vehicleGroup.Id);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsForVehicleGroupAsync_Should_Return_False_When_BillingPlan_For_VehicleGroup_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup otherVehicleGroup = CreateVehicleGroup(companyId, "Outro grupo");
        dbContext.VehicleGroups.Add(otherVehicleGroup);

        BillingPlan billingPlan = CreateBillingPlan(
            name: "Plano de outro grupo",
            companyId: companyId,
            vehicleGroupId: otherVehicleGroup.Id);

        dbContext.BillingPlans.Add(billingPlan);

        await dbContext.SaveChangesAsync();

        Guid vehicleGroupIdWithoutPlan = Guid.NewGuid();

        // act
        bool exists = await billingPlanRepository.ExistsForVehicleGroupAsync(vehicleGroupIdWithoutPlan);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsForVehicleGroupAsync_WithIgnoreId_Should_Return_False_When_Only_BillingPlan_For_VehicleGroup_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        dbContext.VehicleGroups.Add(vehicleGroup);

        BillingPlan billingPlan = CreateBillingPlan(
            name: "Plano único do grupo",
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        dbContext.BillingPlans.Add(billingPlan);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await billingPlanRepository.ExistsForVehicleGroupAsync(vehicleGroup.Id, billingPlan.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio plano como duplicidade para o grupo de veículos.");
    }

    [TestMethod]
    public async Task ExistsForVehicleGroupAsync_WithIgnoreId_Should_Return_True_When_Other_BillingPlan_For_VehicleGroup_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryBillingPlan billingPlanRepository =
            _billingPlanRepository ?? throw new InvalidOperationException("BillingPlan repository not initialized.");

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        dbContext.VehicleGroups.Add(vehicleGroup);

        BillingPlan billingPlan1 = CreateBillingPlan(
            name: "Plano 1",
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        BillingPlan billingPlan2 = CreateBillingPlan(
            name: "Plano 2",
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id);

        dbContext.BillingPlans.Add(billingPlan1);
        dbContext.BillingPlans.Add(billingPlan2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await billingPlanRepository.ExistsForVehicleGroupAsync(vehicleGroup.Id, billingPlan1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro plano de cobrança para o mesmo grupo de veículos.");
    }
}