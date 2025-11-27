using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Infrastructure.Orm.EmployeeModule;

namespace OblivionDrive.Infrastructure.Orm.Shared;
public class OblivionDriveDbContext : IdentityDbContext<User, Role, Guid>, IUnitOfWork
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<Employee> Employees => Set<Employee>();


    private readonly ITenantProvider? _tenantProvider;
    public Guid? CurrentCompanyId { get; }

    public OblivionDriveDbContext(DbContextOptions options,
        ITenantProvider? tenantProvider = null) : base(options)
    {
        _tenantProvider = tenantProvider;
        CurrentCompanyId = _tenantProvider?.CompanyId;
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (_tenantProvider is not null)
        {
            // adicionar todas as entidades com filtro de empresa
            modelBuilder.Entity<Employee>()
            .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);
        }

        // adicionar todas os mapeadores

        modelBuilder.ApplyConfiguration(new EmployeeOrmMApper());

        modelBuilder.Entity<TestEntity>(builder =>
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.ToTable("TestEntities");
        });

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
