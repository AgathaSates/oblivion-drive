using Microsoft.EntityFrameworkCore;

namespace OblivionDrive.Infrastructure.Orm.Shared;
public static class OblivionDriveDbAutoMigrate
{
    public static bool OblivionDriveDb_AutoMigrate(DbContext dbContext)
    {
        var pendingMigrations = dbContext.Database.GetPendingMigrations().Count();

        if (pendingMigrations == 0) return false;

        dbContext.Database.Migrate();

        return true;
    }
}
