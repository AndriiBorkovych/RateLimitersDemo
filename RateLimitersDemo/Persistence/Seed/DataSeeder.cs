using System;
using Bogus;
using RateLimitersDemo.Persistence.Models;

namespace RateLimitersDemo.Persistence.Seed;

public interface IDataSeeder
{
    public void CreateProducts();
}

public class DataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    public void CreateProducts()
    {
        using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        var faker = new Faker<Product>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Price, f => f.Random.Decimal(0.0m, 100.0m))
                .RuleFor(p => p.IsPaid, f => f.Random.Bool(0.4f));

        var products = faker.Generate(50);

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
