namespace InventoryService.Domain;

public class SellResult
{
    public bool Success { get; private init; }
    public int RemainingStock { get; private init; }
    public string? ErrorCode { get; private init; }

    public static SellResult Ok(int remainingStock) => new() { Success = true, RemainingStock = remainingStock };
    public static SellResult NotFound() => new() { Success = false, ErrorCode = "NOT_FOUND" };
    public static SellResult InsufficientStock() => new() { Success = false, ErrorCode = "INSUFFICIENT_STOCK" };
    public static SellResult Conflict() => new() { Success = false, ErrorCode = "CONFLICT" };
}