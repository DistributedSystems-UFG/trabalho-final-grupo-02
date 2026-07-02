using Grpc.Core;
using InventoryService.Protos;

namespace InventoryService.Services;

public class InventoryGrpcService : global::InventoryService.Protos.InventoryService.InventoryServiceBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryGrpcService(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public override async Task<SellResponse> SellProduct(SellRequest request, ServerCallContext context)
    {
        if (request.ProductId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product ID must be greater than 0"));
        if (request.Quantity <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Quantity must be greater than 0"));
        if (string.IsNullOrWhiteSpace(request.Actor))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Actor must not be empty"));

        var result = await _inventoryService.SellAsync(request.ProductId, request.Quantity, request.Actor);
        return new SellResponse
        {
            Success = result.Success,
            ErrorCode = result.ErrorCode ?? string.Empty,
            RemainingStock = result.RemainingStock
        };
    }

    public override async Task<BuyResponse> BuyProduct(BuyRequest request, ServerCallContext context)
    {
        if (request.ProductId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product ID must be greater than 0"));
        if (request.Quantity <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Quantity must be greater than 0"));
        if (string.IsNullOrWhiteSpace(request.Actor))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Actor must not be empty"));

        var result = await _inventoryService.BuyAsync(request.ProductId, request.Quantity, request.Actor);
        return new BuyResponse
        {
            Success = result.Success,
            ErrorCode = result.ErrorCode ?? string.Empty,
            NewStock = result.NewStock
        };
    }

    public override async Task<ProductDto> GetProduct(GetRequest request, ServerCallContext context)
    {
        Console.WriteLine($"[gRPC] Received GetProduct request for ID: {request.ProductId}");
        var product = await _inventoryService.GetProductAsync(request.ProductId) 
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.ProductId} not found"));
        
        return MapToDto(product);
    }

    public override async Task<ListResponse> ListProducts(ListRequest request, ServerCallContext context)
    {
        var products = await _inventoryService.ListProductsAsync(request.Category);
        var response = new ListResponse();
        response.Products.AddRange(products.Select(MapToDto));
        return response;
    }

    private static ProductDto MapToDto(Domain.Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Sku = product.Sku,
        Category = product.Category ?? string.Empty,
        Quantity = product.Quantity,
        Price = (double)product.Price
    };
}