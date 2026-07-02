using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.Domain;

[Table("reconciliation_log")]
public class ReconciliationLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("old_qty")]
    public int OldQty { get; set; }

    [Column("new_qty")]
    public int NewQty { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [MaxLength(100)]
    [Column("worker_id")]
    public string? WorkerId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}