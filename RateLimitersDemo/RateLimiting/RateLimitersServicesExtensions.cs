using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimitersDemo.RateLimiting;

public static class RateLimitersServicesExtensions
{
    public static void AddCustomRateLimiters(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CustomRateLimitOptions>(
            configuration.GetSection(nameof(CustomRateLimitOptions)));

        services.AddRateLimiter(rateLimiterOptions =>
        {
            /*rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext => RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: httpContext.Connection.Id,
                        factory: _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = 1,
                            QueueLimit = 5,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        })),
                PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext => RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        })));*/

            rateLimiterOptions.AddPolicy(PolicyConstants.Fixed, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            rateLimiterOptions.AddPolicy(PolicyConstants.Concurrent, httpContext =>
               RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
                    factory: _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 1,
                        QueueLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            rateLimiterOptions.AddPolicy<string, CustomSlidingRateLimitPolicy>(PolicyConstants.CustomSliding);

            rateLimiterOptions.AddTokenBucketLimiter(PolicyConstants.Token, options =>
            {
                options.TokenLimit = 100;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 5;
                options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                options.TokensPerPeriod = 20;
                options.AutoReplenishment = true;
            });

            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                context.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reasonPhrase);
                reasonPhrase ??= "Fixed window limit was reached";

                context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
                    .CreateLogger("Microsoft.AspNetCore.RateLimitingMiddleware")
                    .LogWarning("Rejected request: {EndpointName}, reason: {ReasonPhrase}, retry after: {RetryInSeconds}s", context.HttpContext.Request.Path, reasonPhrase, retryAfter.TotalSeconds);

                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
            };
        });
    }
}
