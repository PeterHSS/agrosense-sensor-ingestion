using System.Text;
using System.Text.Json;
using Api.Domain.Abstractions.Infrastructure.Messaging;
using Api.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Api.Infrastructure.Messaging;

public class RabbitMQPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "sensor_events";

    public RabbitMQPublisher(RabbitMQSettings rabbitMQSettings)
    {
        var factory = new ConnectionFactory
        {
            HostName = rabbitMQSettings.HostName,
            Port = rabbitMQSettings.Port,
            UserName = rabbitMQSettings.UserName,
            Password = rabbitMQSettings.Password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: ExchangeName, ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;

        _channel.BasicPublish(ExchangeName, routingKey, props, body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
