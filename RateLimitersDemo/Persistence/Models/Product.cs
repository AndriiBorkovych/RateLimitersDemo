using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RateLimitersDemo.Persistence.Models;

public class Product
{
    public int Id { get; set; }
    [StringLength(100)]
    public string Name { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }
    public bool IsPaid { get; set; }
}
