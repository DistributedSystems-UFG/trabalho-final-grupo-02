using Client.Http;

namespace Client.Simulation;

public class LoadSimulator
{
    private readonly InventoryApiClient _client;

    public LoadSimulator(InventoryApiClient client)
    {
        _client = client;
    }

    public async Task SimulateConcurrentSalesAsync(int productId, int quantityPerClient, int totalClients)
    {
        Console.WriteLine($"Starting simulation: {totalClients} concurrent clients buying {quantityPerClient} units of product {productId} each...");
        
        var tasks = new List<Task<SellResponseDto?>>();
        
        for (int i = 0; i < totalClients; i++)
        {
            int clientId = i;
            tasks.Add(Task.Run(() => _client.SellProductAsync(productId, quantityPerClient, $"sim-client-{clientId}")));
        }

        var results = await Task.WhenAll(tasks);

        int successes = results.Count(r => r != null && r.Success);
        int conflicts = results.Count(r => r != null && !r.Success && r.ErrorCode == "CONFLICT");
        int outOfStock = results.Count(r => r != null && !r.Success && r.ErrorCode == "INSUFFICIENT_STOCK");
        int failed = results.Count(r => r == null);

        Console.WriteLine("\n=== Simulation Results ===");
        Console.WriteLine($"Total requests: {totalClients}");
        Console.WriteLine($"Success (HTTP 200): {successes}");
        Console.WriteLine($"Conflict (HTTP 409): {conflicts}");
        Console.WriteLine($"Out of Stock (HTTP 400): {outOfStock}");
        Console.WriteLine($"Failed / Errors: {failed}");
    }
}
