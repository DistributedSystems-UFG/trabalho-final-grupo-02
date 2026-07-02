using System.Net.Http.Json;

namespace Client.Http;

public class InventoryApiClient
{
    private readonly HttpClient _httpClient;

    public InventoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductDto>?> ListProductsAsync(string? category = null)
    {
        var url = "api/inventory";
        if (!string.IsNullOrEmpty(category)) url += $"?category={category}";
        return await _httpClient.GetFromJsonAsync<List<ProductDto>>(url);
    }

    public async Task<SellResponseDto?> SellProductAsync(int productId, int quantity, string actor)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/sell", new { productId, quantity, actor });
        if (response.IsSuccessStatusCode || (int)response.StatusCode == 409 || (int)response.StatusCode == 400)
        {
            return await response.Content.ReadFromJsonAsync<SellResponseDto>();
        }
        return null;
    }

    public async Task<BuyResponseDto?> BuyProductAsync(int productId, int quantity, string actor)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/buy", new { productId, quantity, actor });
        if (response.IsSuccessStatusCode || (int)response.StatusCode == 400)
        {
            return await response.Content.ReadFromJsonAsync<BuyResponseDto>();
        }
        return null;
    }
}

public record ProductDto(int Id, string Name, string Sku, string Category, int Quantity, double Price);
public record SellResponseDto(bool Success, string ErrorCode, int RemainingStock);
public record BuyResponseDto(bool Success, string ErrorCode, int NewStock);
