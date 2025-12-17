using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OblivionDrive.Api.Helpers;
using OblivionDrive.Api.Identity;
using OblivionDrive.Application;
using OblivionDrive.Infrastructure.Orm;
using QuestPDF.Infrastructure;


namespace OblivionDrive.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            QuestPDF.Settings.License = LicenseType.Community;

            builder.Services
                .AddInfraetructureLayer(builder.Configuration)
                .AddApplicationLayer(builder.Logging,builder.Configuration)
                .AddApiMappings(builder.Configuration);


            builder.Services
                .AddControllers()
                .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            builder.Services
                .AddEndpointsApiExplorer()
                .AddIdentityProviderConfig(builder.Configuration);

            builder.Services.AddSwaggerConfig();
            builder.Services.ConfigureCorsPolicy(builder.Environment, builder.Configuration);

            builder.Services.AddRateLimitingConfig();

            var app = builder.Build();

            app.ApplyMigrations();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
