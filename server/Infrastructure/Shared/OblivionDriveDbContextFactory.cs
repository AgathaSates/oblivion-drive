using Microsoft.EntityFrameworkCore;

namespace OblivionDrive.Infrastructure.Orm.Shared;
public static class OblivionDriveDbContextFactory
{
    public static OblivionDriveDbContext CreateDbContext(string connectionString) 
    {
        var options = new DbContextOptionsBuilder<OblivionDriveDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var dbContext = new OblivionDriveDbContext(options);

        return dbContext;
    }
}