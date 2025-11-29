using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm.ServicesModule;
public class ServicesOrmRepository(OblivionDriveDbContext context) : BaseRepository<Service>(context), IRepositoryServices { }
