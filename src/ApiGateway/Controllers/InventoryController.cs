using ApiGateway.GrpcClients;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryGrpcClient _grpcClient;

    public InventoryController(InventoryGrpcClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? category)
    {
        var response = await _grpcClient.ListProductsAsync(category);
        return Ok(response.Products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        Console.WriteLine($"[Gateway] Received Get request for ID: {id}");
        try
        {
            var product = await _grpcClient.GetProductAsync(id);
            return Ok(product);
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            Console.WriteLine($"[Gateway] Product {id} NOT FOUND via gRPC");
            return NotFound();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gateway] Error getting product {id}: {ex.Message}");
            throw;
        }
    }

    [HttpPost("sell")]
    public async Task<IActionResult> Sell([FromBody] SellRequestDto request)
    {
        var response = await _grpcClient.SellAsync(request.ProductId, request.Quantity, request.Actor);
        if (response.Success) return Ok(response);
        
        return response.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(),
            "INSUFFICIENT_STOCK" => BadRequest(response),
            "CONFLICT" => Conflict(response),
            _ => StatusCode(500, response)
        };
    }

    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] BuyRequestDto request)
    {
        var response = await _grpcClient.BuyAsync(request.ProductId, request.Quantity, request.Actor);
        if (response.Success) return Ok(response);
        
        return response.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(),
            _ => StatusCode(500, response)
        };
    }
}

public record SellRequestDto(int ProductId, int Quantity, string Actor);
public record BuyRequestDto(int ProductId, int Quantity, string Actor);
