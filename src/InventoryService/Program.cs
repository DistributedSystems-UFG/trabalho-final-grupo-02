using InventoryService.Data;
using InventoryService.Messaging;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Primary"));
    options.EnableSensitiveDataLogging();
});

// Configuration Options
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection("Demo"));

// Services
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();
builder.Services.AddScoped<IInventoryService, InventoryService.Services.InventoryService>();

var app = builder.Build();

// Verify database connection and data on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var count = await context.Products.CountAsync();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database check: Found {Count} products in the database.", count);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database check failed!");
    }
}

// Configure the HTTP request pipeline.
app.MapGrpcService<InventoryGrpcService>();
app.MapGrpcReflectionService();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();