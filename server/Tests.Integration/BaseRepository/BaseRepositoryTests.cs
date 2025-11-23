using Microsoft.EntityFrameworkCore;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Tests.Integration.Shared;

namespace OblivionDrive.Tests.Integration.BaseRepository;
[TestClass]
[TestCategory("BaseRepository Infrastructure - Integration Tests")]
public sealed class BaseRepositoryTests : TestFixture
{
    private OblivionDriveDbContext _dbContext = null!;
    private BaseRepository<TestEntity> _baseRepository = null!;

    [TestInitialize]
    public void BaseRepositorySetup()
    {
        var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING")
            ?? throw new InvalidOperationException("SQL_CONNECTION_STRING environment variable is not set.");

        _dbContext = OblivionDriveDbContextFactory.CreateDbContext(connectionString);

        _dbContext.Set<TestEntity>().RemoveRange(_dbContext.Set<TestEntity>());
        _dbContext.SaveChanges();

        _baseRepository = new BaseRepository<TestEntity>(_dbContext);
    }

    [TestCleanup]
    public void BaseRepositoryCleanup()
    {
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task AddAsync_Should_Add_Entity_To_Database()
    {
        // arrange
        var entity = new TestEntity("Test entity 1");

        // act
        var id = await _baseRepository.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // assert
        Assert.AreNotEqual(Guid.Empty, id);

        var fromDb = await _dbContext.Set<TestEntity>().SingleOrDefaultAsync(x => x.Id == id);

        Assert.IsNotNull(fromDb);
        Assert.AreEqual("Test entity 1", fromDb!.Name);
    }

    [TestMethod]
    public async Task AddRangeAsync_Should_Add_Multiple_Entities_To_Database()
    {
        // arrange
        var entities = new List<TestEntity>
        {
            new TestEntity("Entity 1"),
            new TestEntity("Entity 2"),
            new TestEntity("Entity 3")
        };

        // act
        await _baseRepository.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();

        // assert
        var fromDb = await _dbContext.Set<TestEntity>().ToListAsync();

        Assert.AreEqual(3, fromDb.Count);
        CollectionAssert.AreEquivalent(
            entities.Select(e => e.Name).ToList(),
            fromDb.Select(e => e.Name).ToList()
        );
    }

    [TestMethod]
    public async Task UpdateAsync_Should_Update_Existing_Entity()
    {
        // arrange
        var original = new TestEntity("Original name");
        await _baseRepository.AddAsync(original);
        await _dbContext.SaveChangesAsync();

        var updated = new TestEntity("Updated name");

        // act
        var result = await _baseRepository.UpdateAsync(original.Id, updated);
        await _dbContext.SaveChangesAsync();

        // assert
        var fromDb = await _dbContext.Set<TestEntity>().SingleOrDefaultAsync(x => x.Id == original.Id);

        Assert.IsNotNull(fromDb);
        Assert.AreEqual("Updated name", fromDb!.Name);
        Assert.AreEqual("Updated name", result.Name);
    }

    [TestMethod]
    public async Task DeleteAsync_Should_Remove_Existing_Entity()
    {
        // arrange
        var entity = new TestEntity("To delete");
        await _baseRepository.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // act
        var deleted = await _baseRepository.DeleteAsync(entity.Id);
        await _dbContext.SaveChangesAsync();

        // assert
        Assert.IsTrue(deleted);

        var fromDb = await _baseRepository.GetByIdAsync(entity.Id);
        Assert.IsNull(fromDb);
    }

    [TestMethod]
    public async Task GetAllAsync_Should_Return_All_Entities()
    {
        // arrange
        var entities = new List<TestEntity>
        {
            new TestEntity("Entity 1"),
            new TestEntity("Entity 2"),
            new TestEntity("Entity 3")
        };

        await _baseRepository.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();

        // act
        var result = await _baseRepository.GetAllAsync();

        // assert
        Assert.AreEqual(3, result.Count);
        CollectionAssert.AreEquivalent(
            entities.Select(e => e.Name).ToList(),
            result.Select(e => e.Name).ToList()
        );
    }

    [TestMethod]
    public async Task GetAllAsync_With_Count_Should_Return_Limited_Results()
    {
        // arrange
        var entities = new List<TestEntity>
        {
            new TestEntity("Entity 1"),
            new TestEntity("Entity 2"),
            new TestEntity("Entity 3")
        };

        await _baseRepository.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();

        // act
        var result = await _baseRepository.GetAllAsync(count: 2);

        // assert
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Entity_When_Id_Exists()
    {
        // arrange
        var entity = new TestEntity("Existing");
        await _baseRepository.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // act
        var fromRepo = await _baseRepository.GetByIdAsync(entity.Id);

        // assert
        Assert.IsNotNull(fromRepo);
        Assert.AreEqual(entity.Id, fromRepo!.Id);
        Assert.AreEqual("Existing", fromRepo.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // arrange
        var nonExistingId = Guid.NewGuid();

        // act
        var fromRepo = await _baseRepository.GetByIdAsync(nonExistingId);

        // assert
        Assert.IsNull(fromRepo);
    }
}

