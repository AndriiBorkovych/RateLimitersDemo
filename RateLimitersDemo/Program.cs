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

builder.Services.AddCustomRateLimiters(builder.Configuration);

var app = builder.Build();

/*using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();

    seeder.CreateProducts();
}*/

//app.UseMiddleware<RequestLoggingMiddleware>();

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
