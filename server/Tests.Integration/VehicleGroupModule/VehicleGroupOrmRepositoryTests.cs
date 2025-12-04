using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Infrastructure.Orm.VehicleGroupModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.VehicleGroupModule;

[TestClass]
[TestCategory("VehicleGroupOrmRepository Infrastructure - Integration Tests")]
public class VehicleGroupOrmRepositoryTests : TestFixture
{
    private static VehicleGroup CreateVehicleGroup(string name, Guid companyId)
    {
        return new VehicleGroup(
            name: name,
            companyId: companyId);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_True_When_VehicleGroup_With_Same_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicleGroup vehicleGroupRepository =
            new VehicleGroupOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();
        string groupName = "Grupo Teste";

        VehicleGroup vehicleGroup = CreateVehicleGroup(groupName, companyId);

        dbContext.VehicleGroups.Add(vehicleGroup);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await vehicleGroupRepository.ExistsByNameAsync(groupName);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_VehicleGroup_With_Name_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicleGroup vehicleGroupRepository =
            new VehicleGroupOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();

        VehicleGroup existingVehicleGroup = CreateVehicleGroup(
            name: "Grupo Existente",
            companyId: companyId);

        dbContext.VehicleGroups.Add(existingVehicleGroup);
        await dbContext.SaveChangesAsync();

        string searchedName = "Outro Grupo";

        // act
        bool exists = await vehicleGroupRepository.ExistsByNameAsync(searchedName);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Name_Is_Empty_Or_Whitespace()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicleGroup vehicleGroupRepository =
            new VehicleGroupOrmRepository(dbContext);

        // act
        bool existsForEmpty = await vehicleGroupRepository.ExistsByNameAsync(string.Empty);
        bool existsForWhitespace = await vehicleGroupRepository.ExistsByNameAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_False_When_Only_VehicleGroup_With_Name_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicleGroup vehicleGroupRepository =
            new VehicleGroupOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();
        string groupName = "Grupo Teste";

        VehicleGroup vehicleGroup = CreateVehicleGroup(groupName, companyId);

        dbContext.VehicleGroups.Add(vehicleGroup);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await vehicleGroupRepository.ExistsByNameAsync(groupName, vehicleGroup.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio grupo como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_True_When_Other_VehicleGroup_With_Same_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicleGroup vehicleGroupRepository =
            new VehicleGroupOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();
        string groupName = "Grupo Duplicado";

        VehicleGroup vehicleGroup1 = CreateVehicleGroup(groupName, companyId);
        VehicleGroup vehicleGroup2 = CreateVehicleGroup(groupName, companyId);

        dbContext.VehicleGroups.Add(vehicleGroup1);
        dbContext.VehicleGroups.Add(vehicleGroup2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await vehicleGroupRepository.ExistsByNameAsync(groupName, vehicleGroup1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro grupo com o mesmo nome.");
    }
}