using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.PartnerModule;

[TestClass]
[TestCategory("PartnerOrmRepository Infrastructure - Integration Tests")]
public class PartnerOrmRepositoryTests : TestFixture
{
    private static Partner CreatePartner(Guid companyId, string name)
    {
        return new Partner(
            name: name,
            companyId: companyId);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_True_When_Partner_With_Same_Name_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryPartner partnerRepository =
            _partnerRepository ?? throw new InvalidOperationException("Partner repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string partnerName = "Parceiro Teste";

        Partner partner = CreatePartner(companyId, partnerName);

        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await partnerRepository.ExistsByNameAsync(partnerName);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Partner_With_Name_Does_Not_Exist()
    {
        // Arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryPartner partnerRepository =
            _partnerRepository ?? throw new InvalidOperationException("Partner repository not initialized.");

        Guid companyId = Guid.NewGuid();

        Partner existingPartner = CreatePartner(companyId, "Parceiro Existente");

        dbContext.Partners.Add(existingPartner);
        await dbContext.SaveChangesAsync();

        string searchedName = "Outro Parceiro";

        // Act
        bool exists = await partnerRepository.ExistsByNameAsync(searchedName);

        // Assert
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_Should_Return_False_When_Name_Is_Empty_Or_Whitespace()
    {
        // Arrange
        IRepositoryPartner partnerRepository =
            _partnerRepository ?? throw new InvalidOperationException("Partner repository not initialized.");

        // Act
        bool existsForEmpty = await partnerRepository.ExistsByNameAsync(string.Empty);
        bool existsForWhitespace = await partnerRepository.ExistsByNameAsync("   ");

        // Assert
        Assert.IsFalse(existsForEmpty);
        Assert.IsFalse(existsForWhitespace);
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_False_When_Only_Partner_With_Name_Is_Self()
    {
        // Arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryPartner partnerRepository =
            _partnerRepository ?? throw new InvalidOperationException("Partner repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string partnerName = "Parceiro Único";

        Partner partner = CreatePartner(companyId, partnerName);

        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await partnerRepository.ExistsByNameAsync(partnerName, partner.Id);

        // Assert
        Assert.IsFalse(exists, "Não deveria considerar o próprio parceiro como duplicidade.");
    }

    [TestMethod]
    public async Task ExistsByNameAsync_WithIgnoreId_Should_Return_True_When_Other_Partner_With_Same_Name_Exists()
    {
        // Arrange
        OblivionDriveDbContext dbContext =
            DbContext ?? throw new InvalidOperationException("DbContext not initialized.");

        IRepositoryPartner partnerRepository =
            _partnerRepository ?? throw new InvalidOperationException("Partner repository not initialized.");

        Guid companyId = Guid.NewGuid();
        string duplicatedPartnerName = "Parceiro Duplicado";

        Partner partner1 = CreatePartner(companyId, duplicatedPartnerName);
        Partner partner2 = CreatePartner(companyId, duplicatedPartnerName);

        dbContext.Partners.Add(partner1);
        dbContext.Partners.Add(partner2);

        await dbContext.SaveChangesAsync();

        // Act
        bool exists = await partnerRepository.ExistsByNameAsync(duplicatedPartnerName, partner1.Id);

        // Assert
        Assert.IsTrue(exists, "Deveria detectar outro parceiro com o mesmo nome.");
    }
}