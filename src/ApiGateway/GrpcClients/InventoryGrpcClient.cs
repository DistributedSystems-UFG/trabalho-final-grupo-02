using InventoryService.Protos;

namespace ApiGateway.GrpcClients;

public class InventoryGrpcClient
{
    private readonly InventoryService.Protos.InventoryService.InventoryServiceClient _client;

    public InventoryGrpcClient(InventoryService.Protos.InventoryService.InventoryServiceClient client)
    {
        _client = client;
    }

    public async Task<SellResponse> SellAsync(int productId, int quantity, string actor)
    {
        return await _client.SellProductAsync(new SellRequest
        {
            ProductId = productId,
            Quantity = quantity,
            Actor = actor
        });
    }

    public async Task<BuyResponse> BuyAsync(int productId, int quantity, string actor)
    {
        return await _client.BuyProductAsync(new BuyRequest
        {
            ProductId = productId,
            Quantity = quantity,
            Actor = actor
        });
    }

    public async Task<ProductDto> GetProductAsync(int productId)
    {
        return await _client.GetProductAsync(new GetRequest { ProductId = productId });
    }

    public async Task<ListResponse> ListProductsAsync(string? category)
    {
        return await _client.ListProductsAsync(new ListRequest { Category = category ?? string.Empty });
    }
}
