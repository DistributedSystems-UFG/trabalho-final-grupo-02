using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.Domain;

[Table("transactions")]
public class Transaction
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("type")]
    public string Type { get; set; } = string.Empty; // "sale", "purchase", "adjustment"

    [Column("quantity")]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("actor")]
    public string Actor { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}