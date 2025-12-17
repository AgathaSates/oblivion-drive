using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OblivionDrive.Application.RentalModule.Services;
using OblivionDrive.Application.Shared;
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
using OblivionDrive.Infrastructure.Orm.Email;
using OblivionDrive.Infrastructure.Orm.EmployeeModule;
using OblivionDrive.Infrastructure.Orm.FuelPriceConfigurationModule;
using OblivionDrive.Infrastructure.Orm.PartnerModule;
using OblivionDrive.Infrastructure.Orm.Pdf.RentalModule;
using OblivionDrive.Infrastructure.Orm.RentalModule;
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
        services.AddScoped<IRepositoryRental, RentalOrmRepository>();


        services.AddScoped<IRentalReceiptPdfGenerator, QuestPdfRentalReceiptPdfGenerator>();
        services.AddScoped<RentalPricingCalculator>();

        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddScoped<IRentalPaymentsReportPdfGenerator, QuestPdfRentalPaymentsReportPdfGenerator>();

        services.AddEntityFrameworkConfig(configuration);

        return services;
    }

    private static void AddEntityFrameworkConfig(
    this IServiceCollection services, IConfiguration configuration)
    {
        string? sqlConnectionString =
         configuration.GetConnectionString("SQL_CONNECTION_STRING")
         ?? configuration["SQL_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(sqlConnectionString))
            throw new Exception("A variável SQL_CONNECTION_STRING não foi fornecida.");

        services.AddDbContext<IUnitOfWork, OblivionDriveDbContext>(dbContextOptions =>
        {
            dbContextOptions.UseSqlServer(sqlConnectionString, sqlServerOptions =>
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                ));
        });

    }
}