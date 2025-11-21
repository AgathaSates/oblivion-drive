using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OblivionDrive.Domain.Shared;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Infrastructure.Orm;
public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfraetructureLayer
        (this IServiceCollection services, IConfiguration configuration)
    {
        // adicionar cada repositóri
        //services.AddScoped<IRepositorioTal, RepositorioTal>();

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