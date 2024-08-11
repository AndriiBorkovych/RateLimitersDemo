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
        var userId = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.Identity.Name
            : "anonymous";
        var partitionKey = $"{userId}-{httpContext.Request.Path}";

        var customOptions = options.Value;

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = customOptions.PermitLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = customOptions.QueueLimit,
                Window = TimeSpan.FromSeconds(customOptions.Window),
                SegmentsPerWindow = customOptions.SegmentsPerWindow
            });
    }
}

public class CustomRateLimitOptions
{
    public int PermitLimit { get; internal set; }
    public int QueueLimit { get; internal set; }
    public double Window { get; internal set; }
    public int SegmentsPerWindow { get; internal set; }
}