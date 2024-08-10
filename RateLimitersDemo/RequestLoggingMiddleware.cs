using System.Diagnostics;

namespace RateLimitersDemo;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private static int _requestCount = 0;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var requestTime = DateTime.Now;
        var requestNumber = Interlocked.Increment(ref _requestCount);

        var clientID = context.Connection.Id ?? "Unknown";

        logger.LogInformation("Request {RequestNumber} started at {StartTime} Connection ID: {ClientIp}.", requestNumber, requestTime, clientID);

        await next(context);

        stopwatch.Stop();
        var responseTime = DateTime.Now;
        var statusCode = context.Response.StatusCode;

        logger.LogInformation("Request {RequestNumber} completed at {ResponseTime} after {ElapsedMilliseconds} ms with status code {StatusCode}", requestNumber, responseTime, stopwatch.ElapsedMilliseconds, statusCode);
    }
}
