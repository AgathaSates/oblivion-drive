using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.PartnerModule;
using OblivionDrive.Domain.RentalModule;
using OblivionDrive.Domain.ServicesModule;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Domain.VehicleGroupModule;
using OblivionDrive.Domain.VehicleModule;
using OblivionDrive.Infrastructure.Orm.BillingPlanModule;
using OblivionDrive.Infrastructure.Orm.ClientModule;
using OblivionDrive.Infrastructure.Orm.CouponModule;
using OblivionDrive.Infrastructure.Orm.DriverModule;
using OblivionDrive.Infrastructure.Orm.EmployeeModule;
using OblivionDrive.Infrastructure.Orm.FuelPriceConfigurationModule;
using OblivionDrive.Infrastructure.Orm.PartnerModule;
using OblivionDrive.Infrastructure.Orm.RentalModule;
using OblivionDrive.Infrastructure.Orm.ServicesModule;
using OblivionDrive.Infrastructure.Orm.VehicleGroupModule;
using OblivionDrive.Infrastructure.Orm.VehicleModule;

namespace OblivionDrive.Infrastructure.Orm.Shared;
public class OblivionDriveDbContext : IdentityDbContext<User, Role, Guid>, IUnitOfWork
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<FuelPriceConfiguration> fuelPrices => Set<FuelPriceConfiguration>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<VehicleGroup> VehicleGroups => Set<VehicleGroup>();
    public DbSet<BillingPlan> BillingPlans => Set<BillingPlan>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Rental> Rentals => Set<Rental>();


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
            modelBuilder.Entity<Employee>()
            .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<FuelPriceConfiguration>()
                .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Service>()
                .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<VehicleGroup>()
                .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<BillingPlan>()
                .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Vehicle>()
                .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Client>()
                .HasQueryFilter(p => 
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Driver>()
               .HasQueryFilter(p =>
               !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Partner>()
                .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Coupon>()
                .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Rental>()
                .HasQueryFilter(p =>
                !CurrentCompanyId.HasValue || p.CompanyId == CurrentCompanyId);

        }

        modelBuilder.ApplyConfiguration(new EmployeeOrmMApper());
        modelBuilder.ApplyConfiguration(new FuelPriceConfigurationOrmMapper());
        modelBuilder.ApplyConfiguration(new ServicesOrmMapper());
        modelBuilder.ApplyConfiguration(new VehicleGroupOrmMapper());
        modelBuilder.ApplyConfiguration(new BillingPlanOrmMapper());
        modelBuilder.ApplyConfiguration(new VehicleOrmMapper());
        modelBuilder.ApplyConfiguration(new ClientOrmMapper());
        modelBuilder.ApplyConfiguration(new DriverOrmMapper());
        modelBuilder.ApplyConfiguration(new PartnerOrmMapper());
        modelBuilder.ApplyConfiguration(new CouponOrmMapper());
        modelBuilder.ApplyConfiguration(new RentalOrmMapper());

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