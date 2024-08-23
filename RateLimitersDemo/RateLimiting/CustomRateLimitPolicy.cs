using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace RateLimitersDemo.RateLimiting;

public class CustomSlidingRateLimitPolicy(
    ILogger<CustomSlidingRateLimitPolicy> logger,
    IOptions<CustomRateLimitOptions> options) : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected =>
        (ctx, token) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status423Locked;
                logger.LogWarning($"Request rejected by {nameof(CustomSlidingRateLimitPolicy)}");
                return ValueTask.CompletedTask;
            };

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var userIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{userIp}-{httpContext.Request.Path}";

        var customOptions = options.Value;

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                // PermitLimit / SegmentsPerWindow = segments count
                // Window / SegmentsPerWindow = duration of each window
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(5),
                SegmentsPerWindow = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
    }
}

public class CustomRateLimitOptions
{
    public int PermitLimit { get; set; }
    public int QueueLimit { get; set; }
    public double Window { get; set; }
    public int SegmentsPerWindow { get; set; }
}