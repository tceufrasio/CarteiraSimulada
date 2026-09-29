using System.Text.Json;
using Carteira.Application.Orders.Notifications;
using RabbitMQ.Client;

namespace Carteira.Infrastructure.Messaging;

public sealed class RabbitMqOrderNotificationPublisher : IOrderNotificationPublisher
{
    public const string QueueName = "carteira.order-notifications";

    private readonly ConnectionFactory? _factory;

    public RabbitMqOrderNotificationPublisher(string? connectionString)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
            };
        }
    }

    public async Task PublishAsync(
        OrderRegisteredNotification notification,
        CancellationToken cancellationToken)
    {
        if (_factory is null)
            throw new InvalidOperationException(
                "Configure a connection string 'RabbitMq'.");

        await using var connection =
            await _factory.CreateConnectionAsync(cancellationToken);

        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        await using var channel =
            await connection.CreateChannelAsync(options, cancellationToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = notification.OrderId.ToString("D"),
            Type = "order.registered.v1"
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(notification);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
