using Microsoft.EntityFrameworkCore;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Domain.VehicleModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Infrastructure.Orm.VehicleModule;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.VehicleModule;

[TestClass]
[TestCategory("VehicleOrmRepository Infrastructure - Integration Tests")]
public class VehicleOrmRepositoryTests : TestFixture
{
    private static VehicleGroup CreateVehicleGroup(Guid companyId, string name = "Grupo Teste")
    {
        return new VehicleGroup(
            name: name,
            companyId: companyId);
    }

    private static Vehicle CreateVehicle(
        Guid companyId,
        Guid vehicleGroupId,
        string licensePlate = "ABC1D23",
        string brand = "Toyota",
        string model = "Corolla",
        string color = "White",
        FuelType fuelType = FuelType.Gasoline,
        decimal fuelTankCapacityInLiters = 55.5m,
        int? year = null)
    {
        return new Vehicle(
            licensePlate: licensePlate,
            brand: brand,
            model: model,
            color: color,
            fuelType: fuelType,
            fuelTankCapacityInLiters: fuelTankCapacityInLiters,
            year: year ?? DateTime.UtcNow.Year,
            vehicleGroupId: vehicleGroupId,
            companyId: companyId);
    }

    [TestMethod]
    public async Task GetByVehicleGroupAsync_Should_Return_Vehicles_Only_From_Specified_Group()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicle vehicleRepository =
            new VehicleOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();

        VehicleGroup targetGroup = CreateVehicleGroup(companyId, "Grupo Alvo");
        VehicleGroup otherGroup = CreateVehicleGroup(companyId, "Outro Grupo");

        dbContext.VehicleGroups.Add(targetGroup);
        dbContext.VehicleGroups.Add(otherGroup);
        await dbContext.SaveChangesAsync();

        Vehicle vehicle1 = CreateVehicle(
            companyId: companyId,
            vehicleGroupId: targetGroup.Id,
            licensePlate: "AAA1A11");

        Vehicle vehicle2 = CreateVehicle(
            companyId: companyId,
            vehicleGroupId: targetGroup.Id,
            licensePlate: "BBB2B22");

        Vehicle vehicleOtherGroup = CreateVehicle(
            companyId: companyId,
            vehicleGroupId: otherGroup.Id,
            licensePlate: "CCC3C33");

        dbContext.Vehicles.Add(vehicle1);
        dbContext.Vehicles.Add(vehicle2);
        dbContext.Vehicles.Add(vehicleOtherGroup);

        await dbContext.SaveChangesAsync();

        // act
        List<Vehicle> result = await vehicleRepository.GetByVehicleGroupAsync(targetGroup.Id);

        // assert
        Assert.AreEqual(2, result.Count, "Deveria retornar apenas veículos do grupo alvo.");
        Assert.IsTrue(result.All(v => v.VehicleGroupId == targetGroup.Id), "Todos os veículos retornados devem pertencer ao grupo alvo.");

        CollectionAssert.AreEquivalent(
            new[] { vehicle1.Id, vehicle2.Id },
            result.Select(v => v.Id).ToArray(),
            "Os veículos retornados deveriam ser exatamente os cadastrados para o grupo alvo.");
    }

    [TestMethod]
    public async Task GetByVehicleGroupAsync_Should_Return_Empty_List_When_No_Vehicles_For_Group()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicle vehicleRepository =
            new VehicleOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();

        VehicleGroup otherGroup = CreateVehicleGroup(companyId, "Grupo com veículo");
        dbContext.VehicleGroups.Add(otherGroup);

        await dbContext.SaveChangesAsync();

        Vehicle vehicleOtherGroup = CreateVehicle(
            companyId: companyId,
            vehicleGroupId: otherGroup.Id,
            licensePlate: "ZZZ9Z99");

        dbContext.Vehicles.Add(vehicleOtherGroup);
        await dbContext.SaveChangesAsync();

        VehicleGroup groupWithoutVehicles = CreateVehicleGroup(companyId, "Grupo sem veículos");
        dbContext.VehicleGroups.Add(groupWithoutVehicles);
        await dbContext.SaveChangesAsync();

        // act
        List<Vehicle> result = await vehicleRepository.GetByVehicleGroupAsync(groupWithoutVehicles.Id);

        // assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count, "Não deveria retornar veículos para um grupo sem veículos cadastrados.");
    }

    [TestMethod]
    public async Task AddPhotoAsync_Should_Set_Photo_For_Vehicle()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicle vehicleRepository =
            new VehicleOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId);
        dbContext.VehicleGroups.Add(vehicleGroup);
        await dbContext.SaveChangesAsync();

        Vehicle vehicle = CreateVehicle(
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id,
            licensePlate: "PHOTO01");

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync();

        byte[] expectedPhoto = { 1, 2, 3, 4, 5 };

        // act
        await vehicleRepository.AddPhotoAsync(vehicle.Id, expectedPhoto);
        await dbContext.SaveChangesAsync();

        Vehicle? updatedVehicle = await dbContext.Vehicles.FindAsync(vehicle.Id);

        // assert
        Assert.IsNotNull(updatedVehicle, "Veículo deveria existir no banco após salvar.");
        Assert.IsNotNull(updatedVehicle!.PhotoBytes, "PhotoBytes não deveria ser nulo após AddPhotoAsync.");
        CollectionAssert.AreEqual(expectedPhoto, updatedVehicle.PhotoBytes!, "PhotoBytes deveria ser igual ao array informado em AddPhotoAsync.");
    }

    [TestMethod]
    public async Task AddPhotoAsync_Should_Override_Previous_Photo()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext
            ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryVehicle vehicleRepository =
            new VehicleOrmRepository(dbContext);

        Guid companyId = Guid.NewGuid();

        VehicleGroup vehicleGroup = CreateVehicleGroup(companyId, "Grupo Photo");
        dbContext.VehicleGroups.Add(vehicleGroup);
        await dbContext.SaveChangesAsync();

        Vehicle vehicle = CreateVehicle(
            companyId: companyId,
            vehicleGroupId: vehicleGroup.Id,
            licensePlate: "PHOTO02");

        byte[] initialPhoto = { 10, 20, 30 };
        vehicle.SetPhoto(initialPhoto);

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync();

        byte[] newPhoto = { 99, 88, 77, 66 };

        // act
        await vehicleRepository.AddPhotoAsync(vehicle.Id, newPhoto);
        await dbContext.SaveChangesAsync();

        Vehicle? updatedVehicle = await dbContext.Vehicles.FindAsync(vehicle.Id);

        // assert
        Assert.IsNotNull(updatedVehicle, "Veículo deveria existir no banco após atualização.");
        Assert.IsNotNull(updatedVehicle!.PhotoBytes, "PhotoBytes não deveria ser nulo após segunda chamada a AddPhotoAsync.");
        CollectionAssert.AreEqual(newPhoto, updatedVehicle.PhotoBytes!, "PhotoBytes deveria ser sobrescrito pela nova foto.");
    }
}
