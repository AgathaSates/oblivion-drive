using Microsoft.EntityFrameworkCore;
using OblivionDrive.Infrastructure.Orm.Shared;
using Testcontainers.MsSql;

namespace OblivionDrive.Tests.Integration.Shared;

[TestClass]
public class TestFixture
{
    protected OblivionDriveDbContext? DbContext;

    // Depois: repositórios
    // protected PartnerRepository? _partnerRepository;
    // protected SomethingRepository? _somethingRepository;

    protected static MsSqlContainer? DatabaseContainer;

    protected OblivionDriveApiFactory ApiFactory = null!;
    protected HttpClient HttpClient = null!;

    [AssemblyInitialize]
    public static async Task AssemblySetup(TestContext _)
    {
        DatabaseContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("StrongPassword1!")
            .WithCleanUp(true)
            .Build();

        await DatabaseContainer.StartAsync();
    }

    [TestInitialize]
    public void Setup()
    {
        if (DatabaseContainer is null)
            throw new InvalidOperationException("O banco de dados não foi inicializado.");

        string connectionString = DatabaseContainer.GetConnectionString();

        using (var dbContext = OblivionDriveDbContextFactory.CreateDbContext(connectionString))
        {
            dbContext.Database.Migrate();

            dbContext.UserRoles.RemoveRange(dbContext.UserRoles);
            dbContext.Users.RemoveRange(dbContext.Users);
            dbContext.Roles.RemoveRange(dbContext.Roles);

            // Futuro: limpar outras entidades
            // dbContext.Partners.RemoveRange(dbContext.Partners);

            dbContext.SaveChanges();
        }

        Environment.SetEnvironmentVariable("SQL_CONNECTION_STRING", connectionString);
        Environment.SetEnvironmentVariable("AUTOMAPPER_LICENSE_KEY", "integration-tests-automapper-license");
        Environment.SetEnvironmentVariable("REDIS_CONNECTION_STRING", "localhost:6379,abortConnect=false");
        Environment.SetEnvironmentVariable("NEWRELIC_LICENSE_KEY", "integration-tests-newrelic-license");
        Environment.SetEnvironmentVariable("JWT_GENERATION_KEY", "integration-tests-jwt-key-0123456789ABCDEF");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE_DOMAIN", "https://localhost:7013");
        Environment.SetEnvironmentVariable("CORS_ALLOWED_ORIGINS", "https://localhost:4200");

        ApiFactory = new OblivionDriveApiFactory(connectionString);
        HttpClient = ApiFactory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        HttpClient.Dispose();
        ApiFactory.Dispose();
    }

    [AssemblyCleanup]
    public static async Task AssemblyTeardown()
    {
        if (DatabaseContainer is null)
            return;

        await DatabaseContainer.StopAsync();
        await DatabaseContainer.DisposeAsync();
    }
}