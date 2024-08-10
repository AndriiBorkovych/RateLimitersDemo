using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RateLimitersDemo;
using RateLimitersDemo.Persistence;
using RateLimitersDemo.Persistence.Seed;
using RateLimitersDemo.RateLimiting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IDataSeeder, DataSeeder>();

builder.Services.AddSerilog((_, lc) => lc.ReadFrom.Configuration(builder.Configuration).WriteTo.Seq("http://localhost:5341"));

builder.Services.AddRateLimiter(rateLimiterOptions =>
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

var app = builder.Build();

/*using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();

    seeder.CreateProducts();
}*/

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors(builder =>
        builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

app.Run();
