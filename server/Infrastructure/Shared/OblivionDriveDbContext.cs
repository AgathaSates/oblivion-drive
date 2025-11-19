using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Infrastructure.Orm.Shared;
public class OblivionDriveDbContext(DbContextOptions options, ITenantProvider? _tenantProvider = null)
    : IdentityDbContext<User, Role, Guid>(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (_tenantProvider is not null)
        {
            var currentUserId = _tenantProvider.UserId;

            // adicionar todas as entidades
            //modelBuilder.Entity<Partner>()
            //    .HasQueryFilter(x => x.UserId == currentUserId);
        }

        // adicionar todas os mapeadores

        //modelBuilder.ApplyConfiguration(new MapperPartinerOrm());

        base.OnModelCreating(modelBuilder);
    }

    public async Task CommitAsync()
    {
        await SaveChangesAsync();
    }

    public async Task RollbackAsync()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;

                case EntityState.Modified:
                    entry.State = EntityState.Unchanged;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Unchanged;
                    break;
            }
        }

        await Task.CompletedTask;
    }
}
