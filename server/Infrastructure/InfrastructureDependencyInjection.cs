using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OblivionDrive.Domain.BillingPlanModule;
using OblivionDrive.Domain.ClientModule;
using OblivionDrive.Domain.CouponModule;
using OblivionDrive.Domain.DriverModule;
using OblivionDrive.Domain.EmployeeModule;
using OblivionDrive.Domain.FuelPriceConfigurationModule;
using OblivionDrive.Domain.PartnerModule;
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
using OblivionDrive.Infrastructure.Orm.ServicesModule;
using OblivionDrive.Infrastructure.Orm.Shared;
using OblivionDrive.Infrastructure.Orm.VehicleGroupModule;
using OblivionDrive.Infrastructure.Orm.VehicleModule;

namespace OblivionDrive.Infrastructure.Orm;
public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfraetructureLayer
        (this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IRepositoryEmployee, EmployeeOrmRepository>();
        services.AddScoped<IRepositoryFuelPriceSettings, FuelPriceConfigurationOrmRepository>();
        services.AddScoped<IRepositoryServices, ServicesOrmRepository>();
        services.AddScoped<IRepositoryVehicleGroup, VehicleGroupOrmRepository>();
        services.AddScoped<IRepositoryBillingPlan, BillingPlanOrmRepository>();
        services.AddScoped<IRepositoryVehicle, VehicleOrmRepository>();
        services.AddScoped<IRepositoryClient, ClientOrmRepository>();
        services.AddScoped<IRepositoryDriver, DriverOrmRepository>();
        services.AddScoped<IRepositoryPartner, PartnerOrmRepository>();
        services.AddScoped<IRepositoryCoupon, CouponOrmRepository>();


        services.AddEntityFrameworkConfig(configuration);

        return services;
    }

    private static void AddEntityFrameworkConfig(
    this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["SQL_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("A variável SQL_CONNECTION_STRING não foi fornecida.");

        services.AddDbContext<IUnitOfWork, OblivionDriveDbContext>(options =>
            options.UseSqlServer(connectionString, (opt) => opt.EnableRetryOnFailure(3)));
    }
}