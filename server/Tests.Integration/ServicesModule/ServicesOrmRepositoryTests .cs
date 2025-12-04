using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.ServicesModule;

[TestClass]
[TestCategory("ServicesOrmRepository Infrastructure - Integration Tests")]
public class ServicesOrmRepositoryTests : TestFixture
{
    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_True_When_Service_With_Same_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryServices servicesRepository = _servicesRepository ?? throw new InvalidOperationException("Services repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string serviceName = "Serviço Teste";

        Service service = new Service(
            name: serviceName,
            price: 100m,
            chargeType: default,
            companyId: companyId
        );

        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await servicesRepository.ExistsByNameAsync(serviceName);

        // assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Service_With_Name_Does_Not_Exist()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryServices servicesRepository = _servicesRepository ?? throw new InvalidOperationException("Services repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Service existingService = new Service(
            name: "Serviço Existente",
            price: 150m,
            chargeType: default,
            companyId: companyId
        );

        dbContext.Services.Add(existingService);
        await dbContext.SaveChangesAsync();

        string searchedName = "Outro Serviço";

        // act
        bool exists = await servicesRepository.ExistsByNameAsync(searchedName);

        // assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Name_Is_Empty_Or_Whitespace()
    {
        // arrange
        IRepositoryServices servicesRepository = _servicesRepository ?? throw new InvalidOperationException("Services repository not initialized.");

        // act
        bool existsForEmpty = await servicesRepository.ExistsByNameAsync(string.Empty);
        bool existsForWhitespace = await servicesRepository.ExistsByNameAsync("   ");

        // assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_False_When_Only_Service_With_Name_Is_Self()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryServices servicesRepository = _servicesRepository ?? throw new InvalidOperationException("Services repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string serviceName = "Serviço Teste";

        Service service = new Service(
            name: serviceName,
            price: 120m,
            chargeType: default,
            companyId: companyId
        );

        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync();

        // act
        bool exists = await servicesRepository.ExistsByNameAsync(serviceName, service.Id);

        // assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio serviço como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_True_When_Other_Service_With_Same_Name_Exists()
    {
        // arrange
        OblivionDriveDbContext dbContext = DbContext ?? throw new InvalidOperationException("DbContext not initialized.");
        IRepositoryServices servicesRepository = _servicesRepository ?? throw new InvalidOperationException("Services repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string serviceName = "Serviço Duplicado";

        Service service1 = new Service(
            name: serviceName,
            price: 100m,
            chargeType: default,
            companyId: companyId
        );

        dbContext.Services.Add(service1);

        Service service2 = new Service(
            name: serviceName,
            price: 200m,
            chargeType: default,
            companyId: companyId
        );

        dbContext.Services.Add(service2);

        await dbContext.SaveChangesAsync();

        // act
        bool exists = await servicesRepository.ExistsByNameAsync(serviceName, service1.Id);

        // assert
        Assert.IsTrue(exists, "Deveria detectar outro serviço com o mesmo nome.");
    }
}