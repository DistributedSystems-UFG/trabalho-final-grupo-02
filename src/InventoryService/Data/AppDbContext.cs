using Microsoft.EntityFrameworkCore;
using InventoryService.Domain;

namespace InventoryService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public virtual DbSet<Product> Products => Set<Product>();
    public virtual DbSet<Transaction> Transactions => Set<Transaction>();
    public virtual DbSet<ReconciliationLog> ReconciliationLogs => Set<ReconciliationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.Property(p => p.Id)
                  .HasColumnName("id");

            entity.Property(p => p.Price)
                  .HasColumnType("decimal(18,2)");

            entity.HasIndex(p => p.Sku)
                  .IsUnique();

            entity.HasIndex(p => p.Category)
                  .HasDatabaseName("idx_products_category");

            // Map the Version property to the hidden PostgreSQL 'xmin' system column
            entity.Property(p => p.Version)
                  .HasColumnName("xmin")
                  .HasColumnType("xid")
                  .ValueGeneratedOnAddOrUpdate()
                  .IsConcurrencyToken();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");

            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(t => t.ProductId)
                  .HasConstraintName("fk_transactions_product_id");

            entity.HasIndex(t => t.ProductId)
                  .HasDatabaseName("idx_transactions_product_id");

            entity.HasIndex(t => t.CreatedAt)
                  .HasDatabaseName("idx_transactions_created_at");
        });

        modelBuilder.Entity<ReconciliationLog>(entity =>
        {
            entity.ToTable("reconciliation_log");

            entity.HasOne<Product>()
                  .WithMany()
                  .HasForeignKey(r => r.ProductId)
                  .HasConstraintName("fk_reconciliation_log_product_id");
        });
    }
}