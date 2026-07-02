using Client.Http;
using Client.Simulation;

var httpClient = new HttpClient { BaseAddress = new Uri("http://35.171.130.40:80/") };
var client = new InventoryApiClient(httpClient);
var simulator = new LoadSimulator(client);

Console.WriteLine("=== Inventory System CLI ===");

while (true)
{
    Console.WriteLine("\nOptions: [1] List [2] Sell [3] Buy [4] Simulate Load [q] Quit");
    var choice = Console.ReadLine();

    if (choice == "q") break;

    switch (choice)
    {
        case "1":
            var products = await client.ListProductsAsync();
            if (products != null)
            {
                foreach (var p in products.OrderBy(p => p.Id))
                    Console.WriteLine($"{p.Id}: {p.Name} ({p.Quantity} in stock)");
            }
            break;
        case "2":
            Console.Write("Product ID: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) break;
            Console.Write("Quantity: ");
            if (!int.TryParse(Console.ReadLine(), out int qty)) break;
            
            var result = await client.SellProductAsync(id, qty, "cli-user");
            if (result != null)
            {
                if (result.Success) Console.WriteLine($"Success! Remaining: {result.RemainingStock}");
                else Console.WriteLine($"Failed: {result.ErrorCode}");
            }
            break;
        case "3":
            Console.Write("Product ID: ");
            if (!int.TryParse(Console.ReadLine(), out int buyId)) break;
            Console.Write("Quantity: ");
            if (!int.TryParse(Console.ReadLine(), out int buyQty)) break;
            
            var buyResult = await client.BuyProductAsync(buyId, buyQty, "supplier-bot");
            if (buyResult != null)
            {
                if (buyResult.Success) Console.WriteLine($"Success! New Stock: {buyResult.NewStock}");
                else Console.WriteLine("Failed to buy product.");
            }
            break;
        case "4":
            Console.Write("Product ID to simulate: ");
            if (!int.TryParse(Console.ReadLine(), out int simId)) break;
            
            Console.Write("Quantity per client: ");
            if (!int.TryParse(Console.ReadLine(), out int simQty)) break;
            
            Console.Write("Total concurrent clients: ");
            if (!int.TryParse(Console.ReadLine(), out int simClients)) break;

            Console.WriteLine($"\nSimulating {simClients} concurrent clients selling {simQty} unit(s) of Product {simId}...");
            await simulator.SimulateConcurrentSalesAsync(simId, simQty, simClients);
            break;
    }
}
