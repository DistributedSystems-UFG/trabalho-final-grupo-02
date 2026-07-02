using InventoryService.Data;
using InventoryService.Domain;
using InventoryService.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryService.Services;

public interface IInventoryService
{
    Task<SellResult> SellAsync(int productId, int quantity, string actor);
    Task<BuyResult> BuyAsync(int productId, int quantity, string actor);
    Task<Product?> GetProductAsync(int productId);
    Task<List<Product>> ListProductsAsync(string? category);
}

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<InventoryService> _logger;
    private readonly DemoOptions _demoOptions;

    public InventoryService(
        AppDbContext db,
        IEventPublisher eventPublisher,
        ILogger<InventoryService> logger,
        IOptions<DemoOptions> demoOptions)
    {
        _db = db;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _demoOptions = demoOptions.Value;
    }

    /// <summary>
    /// Executes a sale operation with optimistic concurrency control.
    /// Prevents lost updates when multiple clients sell the same product simultaneously.
    /// </summary>
    public async Task<SellResult> SellAsync(int productId, int quantity, string actor)
    {
        const int maxAttempts = 3;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                // Always fetch fresh from DB to get the latest RowVersion
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null) return SellResult.NotFound();
                if (product.Quantity < quantity) return SellResult.InsufficientStock();

                // DEMO ONLY — widens conflict window to make race condition visible during presentation
                if (_demoOptions.ArtificialDelayMs > 0)
                    await Task.Delay(_demoOptions.ArtificialDelayMs);

                product.Quantity -= quantity;
                product.UpdatedAt = DateTimeOffset.UtcNow;

                _db.Transactions.Add(new Transaction
                {
                    ProductId = productId,
                    Type = "sale",
                    Quantity = quantity,
                    Actor = actor,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                // EF Core will use RowVersion in the WHERE clause of the UPDATE statement
                await _db.SaveChangesAsync();

                // Downstream: Dashboard browser shows "product.sold" alerts in real time
                await _eventPublisher.PublishAsync("inventory.product.sold", new
                {
                    Event = "product.sold",
                    ProductId = productId,
                    Quantity = quantity,
                    RemainingStock = product.Quantity,
                    Actor = actor,
                    Timestamp = DateTimeOffset.UtcNow
                });

                // Downstream: Maintenance Worker (Python) consumes "inventory.stock.low" to trigger restock logic
                if (product.Quantity <= product.MinAlert)
                {
                    await _eventPublisher.PublishAsync("inventory.stock.low", new
                    {
                        Event = "stock.low",
                        ProductId = productId,
                        CurrentStock = product.Quantity,
                        Threshold = product.MinAlert,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                    _logger.LogWarning("Stock for Product {ProductId} dropped to {Qty} (Below Threshold). Event 'stock.low' emitted.", productId, product.Quantity);
                }

                _logger.LogInformation("Successfully processed SELL for Product {ProductId}: -{Qty} units. Remaining: {Remaining}", productId, quantity, product.Quantity);
                return SellResult.Ok(product.Quantity);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Discard stale tracked entity before retrying
                _db.ChangeTracker.Clear();
                _logger.LogWarning("Concurrency conflict on product {ProductId}, attempt {Attempt}", productId, attempt + 1);
                
                // Exponential backoff with jitter
                await Task.Delay(Random.Shared.Next(10, 50) * (attempt + 1));
            }
        }

        return SellResult.Conflict();
    }

    public async Task<BuyResult> BuyAsync(int productId, int quantity, string actor)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return BuyResult.NotFound();

        product.Quantity += quantity;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Transactions.Add(new Transaction
        {
            ProductId = productId,
            Type = "purchase",
            Quantity = quantity,
            Actor = actor,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();

        // Downstream: Dashboard shows "product.bought" event
        await _eventPublisher.PublishAsync("inventory.product.bought", new
        {
            Event = "product.bought",
            ProductId = productId,
            Quantity = quantity,
            NewStock = product.Quantity,
            Actor = actor,
            Timestamp = DateTimeOffset.UtcNow
        });

        _logger.LogInformation("Successfully processed BUY for Product {ProductId}: +{Qty} units. New Stock: {NewStock}", productId, quantity, product.Quantity);
        return BuyResult.Ok(product.Quantity);
    }

    public async Task<Product?> GetProductAsync(int productId)
    {
        _logger.LogInformation("Searching for product with ID: {ProductId}", productId);
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found in database.", productId);
        }
        return product;
    }

    public async Task<List<Product>> ListProductsAsync(string? category)
    {
        var query = _db.Products.AsNoTracking();
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
        }
        return await query.ToListAsync();
    }
}