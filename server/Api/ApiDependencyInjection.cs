using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OblivionDrive.Application;
using OblivionDrive.Domain.AuthenticationModule;
using OblivionDrive.Infrastructure.Orm.Shared;

namespace OblivionDrive.Api;

public static class ApiDependencyInjection
{
    public static IServiceCollection AddApiMappings(
      this IServiceCollection services,
      IConfiguration configuration)
    {
        var applicationAssembly = typeof(ApplicationDependencyInjection).Assembly;
        var apiAssembly = typeof(Program).Assembly;

        var automapperLicenseKey = configuration["AUTOMAPPER_LICENSE_KEY"];

        if (string.IsNullOrWhiteSpace(automapperLicenseKey))
            throw new Exception("A variável AUTOMAPPER_LICENSE_KEY não foi fornecida.");

        services.AddAutoMapper(cfg =>
        {
            cfg.LicenseKey = automapperLicenseKey;
        }, applicationAssembly, apiAssembly);

        return services;
    }

    public static void AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Oblivion Drive API", Version = "v1" });

            options.MapType<TimeSpan>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "time-span",
                Example = new Microsoft.OpenApi.Any.OpenApiString("00:00:00")
            });

            options.MapType<DateTime>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "date-time",
                Example = new Microsoft.OpenApi.Any.OpenApiString("2025-09-18T00:00:00")
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Informe o token JWT no padrão \"Bearer {token}\"",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            });

            options.EnableAnnotations();
        });
    }

    public static void ConfigureCorsPolicy(
    this IServiceCollection services,
    IWebHostEnvironment environment,
    IConfiguration configuration
)
    {
        services.AddCors(options =>
        {
            if (environment.IsDevelopment())
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            }
            else
            {
                var allowedOriginsString = configuration["CORS_ALLOWED_ORIGINS"];

                if (string.IsNullOrWhiteSpace(allowedOriginsString))
                    throw new Exception("A variável de ambiente \"CORS_ALLOWED_ORIGINS\" não foi fornecida.");

                var allowedOrigins = allowedOriginsString
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.TrimEnd('/'))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                options.AddDefaultPolicy(policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            }
        });
    }

    public static void ApplyMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OblivionDriveDbContext>();

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        executionStrategy.Execute(() =>
        {
            dbContext.Database.Migrate();
            CreateRoles(scope.ServiceProvider);
        });
    }

    private static void CreateRoles(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<Role>>();

        string[] roles =
        [
            "Company",
            "Employee"
        ];

        foreach (string roleName in roles)
        {
            bool exists = roleManager.RoleExistsAsync(roleName)
                .GetAwaiter()
                .GetResult();

            if (!exists)
            {
                var role = new Role
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                };

                roleManager.CreateAsync(role)
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}