using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.Domain;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("sku")]
    public string Sku { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("min_alert")]
    public int MinAlert { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public uint Version { get; set; }
}