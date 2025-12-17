using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace OblivionDrive.Application;
public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationLayer
        (this IServiceCollection services,
        ILoggingBuilder logging, IConfiguration configuration)
    {
        var assembly = typeof(ApplicationDependencyInjection).Assembly;
        var licensekey = configuration["AUTOMAPPER_LICENSE_KEY"];
        var redisConnectionString = configuration["REDIS_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(licensekey))
            throw new Exception("A variável AUTOMAPPER_LICENSE_KEY não foi fornecida.");

        if (string.IsNullOrWhiteSpace(redisConnectionString))
            throw new Exception("A variável REDIS_CONNECTION_STRING não foi fornecida.");


        services.AddSerilogConfig(logging, configuration);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.LicenseKey = licensekey;
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddStackExchangeRedisCache(opt =>
        {
            opt.Configuration = redisConnectionString;
            opt.InstanceName = "oblivion-drive-api";
        });

        return services;
    }

    private static void AddSerilogConfig(this IServiceCollection services, 
        ILoggingBuilder logging, IConfiguration configuration)
    {
        var licenseKey = configuration["NEWRELIC_LICENSE_KEY"];

        if (string.IsNullOrWhiteSpace(licenseKey))
            throw new Exception("A variável NEWRELIC_LICENSE_KEY não foi fornecida.");

        var pathAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var pathFileLogs = Path.Combine(pathAppData, "oblivion-drive-api", "erro.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(pathFileLogs, LogEventLevel.Error)
            .WriteTo.NewRelicLogs(
                endpointUrl: "https://log-api.newrelic.com/log/v1",
                applicationName: "oblivion-drive-api",
                licenseKey: licenseKey
            )
            .CreateLogger();

        logging.ClearProviders();
        services.AddSerilog();
    }
}
