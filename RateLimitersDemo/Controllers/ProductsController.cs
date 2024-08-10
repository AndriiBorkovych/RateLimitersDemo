using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RateLimitersDemo.Persistence;
using RateLimitersDemo.Persistence.Models;
using RateLimitersDemo.RateLimiting;

namespace RateLimitersDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new { id = product.Id }, product);
    }

    [HttpGet("All")]
    [EnableRateLimiting(PolicyConstants.Fixed)]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return await context.Products.ToListAsync();
    }

    [HttpGet("Paid")]
    [EnableRateLimiting(PolicyConstants.Fixed)]
    public async Task<ActionResult<IEnumerable<Product>>> GetPaid()
    {
        return await context.Products.Where(p => p.IsPaid).ToListAsync();
    }

    [HttpGet("Unpaid")]
    [EnableRateLimiting(PolicyConstants.Fixed)]
    public async Task<ActionResult<IEnumerable<Product>>> GetUnpaid()
    {
        return await context.Products.Where(p => !p.IsPaid).ToListAsync();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync();

        return NoContent();
    }
}