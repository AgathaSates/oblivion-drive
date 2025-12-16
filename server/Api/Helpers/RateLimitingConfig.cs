using System.Security.Claims;
using System.Threading.RateLimiting;

namespace OblivionDrive.Api.Helpers;

public static class RateLimitingConfig
{
    public const string RentalReceiptEmailPolicyName = "RentalReceiptEmailPolicy";

    public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers["Retry-After"] =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    message = "Limite de envios atingido: máximo de 3 envios por minuto para este aluguel."
                }, cancellationToken);
            };

            options.AddPolicy(RentalReceiptEmailPolicyName, httpContext =>
            {
                string userId =
                    httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";

                string rentalId =
                    httpContext.GetRouteValue("rentalId")?.ToString() ?? "unknown-rental";

                string partitionKey = $"{userId}:{rentalId}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }
}