using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InventoryService.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T payload);
}

/// <summary>
/// Handles publishing events to RabbitMQ.
/// MANDATE (from GEMINI.md): Always publish events AFTER SaveChangesAsync() returns successfully.
/// </summary>
public class EventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMQOptions _options;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "inventory";

    public EventPublisher(IOptions<RabbitMQOptions> options)
    {
        _options = options.Value;
    }

    private void EnsureChannel()
    {
        if (_channel != null) return;

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Exchange: inventory, Type: topic, Durable: true
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync<T>(string routingKey, T payload)
    {
        EnsureChannel();

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel!.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) _channel.Close();
        if (_connection != null) await Task.Run(() => _connection.Close());
        GC.SuppressFinalize(this);
    }
}