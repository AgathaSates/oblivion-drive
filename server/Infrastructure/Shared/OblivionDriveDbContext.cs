using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.Shared;

namespace OblivionDrive.Infrastructure.Orm.Shared;
public class OblivionDriveDbContext : IdentityDbContext<User, Role, Guid>, IUnitOfWork
{
    private readonly ITenantProvider? _tenantProvider;

    public Guid? CurrentCompanyId { get; }

    public OblivionDriveDbContext(
        DbContextOptions options,
        ITenantProvider? tenantProvider = null)
        : base(options)
    {
        _tenantProvider = tenantProvider;
        CurrentCompanyId = _tenantProvider?.CompanyId;
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (_tenantProvider is not null)
        {

            // adicionar todas as entidades
            //modelBuilder.Entity<Partner>()
            //.HasQueryFilter(p =>
            //    !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);
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
