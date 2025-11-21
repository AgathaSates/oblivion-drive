using System.Text.Json.Serialization;
using OblivionDrive.Api.Identity;
using OblivionDrive.Application;
using OblivionDrive.Infrastructure.Orm;

namespace OblivionDrive.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddInfraetructureLayer(builder.Configuration)
                .AddApplicationLayer(builder.Logging,builder.Configuration);

            builder.Services
                .AddControllers()
                .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            builder.Services
                .AddEndpointsApiExplorer()
                .AddIdentityProviderConfig(builder.Configuration);

            builder.Services.AddSwaggerConfig();
            builder.Services.ConfigureCorsPolicy(builder.Environment, builder.Configuration);

            var app = builder.Build();

            app.ApplyMigrations();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
