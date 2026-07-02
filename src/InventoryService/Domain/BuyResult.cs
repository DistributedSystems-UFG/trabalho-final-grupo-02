namespace InventoryService.Domain;

public class BuyResult
{
    public bool Success { get; private init; }
    public int NewStock { get; private init; }
    public string? ErrorCode { get; private init; }

    public static BuyResult Ok(int newStock) => new() { Success = true, NewStock = newStock };
    public static BuyResult NotFound() => new() { Success = false, ErrorCode = "NOT_FOUND" };
}