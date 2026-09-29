using System.Text.Json;
using Carteira.Application.Orders.Notifications;
using Carteira.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace Carteira.IntegrationTests;

public class RabbitMqOrderNotificationPublisherTests
{
    [Fact]
    public async Task PublishAsync_DeliversPersistentNotificationToTestQueue()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CARTEIRA_TEST_RABBITMQ")
            ?? throw new InvalidOperationException(
                "Configure CARTEIRA_TEST_RABBITMQ para o broker de testes.");

        var queueName = "carteira.tests." + Guid.NewGuid().ToString("N");
        var notification = new OrderRegisteredNotification(
            Guid.NewGuid(), "PETR4", DateTimeOffset.UtcNow);

        try
        {
            var publisher = new RabbitMqOrderNotificationPublisher(
                connectionString, queueName);

            await publisher.PublishAsync(notification, CancellationToken.None);

            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            var delivery = await channel.BasicGetAsync(
                queueName, autoAck: true);

            Assert.NotNull(delivery);
            Assert.Equal(
                notification.OrderId.ToString("D"),
                delivery.BasicProperties.MessageId);
            Assert.True(delivery.BasicProperties.Persistent);

            var received =
                JsonSerializer.Deserialize<OrderRegisteredNotification>(
                    delivery.Body.Span);

            Assert.Equal(notification, received);
        }
        finally
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeleteAsync(
                queueName, ifUnused: false, ifEmpty: false);
        }
    }
}
