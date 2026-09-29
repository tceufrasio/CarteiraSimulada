using RabbitMQ.Client;

namespace Carteira.Infrastructure.Messaging;

public static class OrderNotificationTopology
{
    public const string MainQueue =
        RabbitMqOrderNotificationPublisher.QueueName;

    public const string RetryQueue =
        "carteira.order-notifications.retry";

    public const string FailedQueue =
        "carteira.order-notifications.failed";

    public static async Task DeclareAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            queue: MainQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = 5000,
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = MainQueue
            },
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: FailedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
