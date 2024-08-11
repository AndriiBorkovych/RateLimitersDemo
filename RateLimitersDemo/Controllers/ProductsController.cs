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
    [HttpPost("Create")]
    public async Task<IActionResult> Create(Product product)
    {
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new { id = product.Id }, product);
    }

    [HttpGet("GetById/{id}")]
    [EnableRateLimiting(PolicyConstants.Fixed)]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        return product;
    }

    [HttpGet("All")]
    [EnableRateLimiting(PolicyConstants.Fixed)]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return await context.Products.ToListAsync();
    }

    [HttpGet("Paid")]
    [EnableRateLimiting(PolicyConstants.Concurrent)]
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

    [HttpPost("Pay/{id}")]
    public async Task<IActionResult> Pay(int id)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        if (product.IsPaid)
        {
            return BadRequest("Product is already paid");
        }

        product.IsPaid = true;

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync();

        return NoContent();
    }
}