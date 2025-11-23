using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using OblivionDrive.Api;

namespace OblivionDrive.Tests.Integration.Shared;

public sealed class OblivionDriveApiFactory : WebApplicationFactory<Program>
{
    private readonly string sqlConnectionString;

    public OblivionDriveApiFactory(string sqlConnectionString)
    {
        this.sqlConnectionString = sqlConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                // BD aponta pro container de testes
                ["SQL_CONNECTION_STRING"] = sqlConnectionString,

                // Chaves “fake” só pra satisfazer as DI do Application
                ["AUTOMAPPER_LICENSE_KEY"] = "integration-tests-automapper-license",
                ["REDIS_CONNECTION_STRING"] = "localhost:6379,abortConnect=false",
                ["NEWRELIC_LICENSE_KEY"] = "integration-tests-newrelic-license",

                // JWT – chave e audiência compatíveis com a sua configuração
                ["JWT_GENERATION_KEY"] = "integration-tests-jwt-key-0123456789ABCDEF",
                ["JWT_AUDIENCE_DOMAIN"] = "https://localhost:7013",

                // CORS (não é crítico pros testes, mas deixo configurado)
                ["CORS_ALLOWED_ORIGINS"] = "https://localhost:4200"
            };

            configBuilder.AddInMemoryCollection(settings!);
        });
    }
}