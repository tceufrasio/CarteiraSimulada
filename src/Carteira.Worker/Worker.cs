using System.Text.Json;
using Carteira.Application.Orders.Notifications;
using Carteira.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Carteira.Worker;

public sealed class Worker : BackgroundService
{
    private const int MaxRetries = 3;

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<Worker> _logger;
    private readonly ConnectionFactory _factory;

    public Worker(
        IServiceScopeFactory scopes,
        IConfiguration configuration,
        ILogger<Worker> logger)
    {
        _scopes = scopes;
        _logger = logger;

        var connectionString = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException(
                "Configure a connection string 'RabbitMq'.");

        _factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection =
                    await _factory.CreateConnectionAsync(stoppingToken);

                var options = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true);

                await using var channel =
                    await connection.CreateChannelAsync(options, stoppingToken);

                await OrderNotificationTopology.DeclareAsync(
                    channel, stoppingToken);

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (_, delivery) =>
                {
                    var body = delivery.Body.ToArray();

                    try
                    {
                        var notification =
                            JsonSerializer.Deserialize<OrderRegisteredNotification>(body)
                            ?? throw new JsonException("Empty notification.");

                        await using var scope = _scopes.CreateAsyncScope();
                        var processor = scope.ServiceProvider
                            .GetRequiredService<ProcessOrderNotification>();

                        var isNew = await processor.HandleAsync(
                            notification, stoppingToken);

                        await channel.BasicAckAsync(
                            delivery.DeliveryTag, false, stoppingToken);

                        _logger.LogInformation(
                            "Order {OrderId}: {Result}",
                            notification.OrderId,
                            isNew ? "processed" : "already processed");
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        // The unacknowledged delivery returns when the channel closes.
                    }
                    catch (Exception exception)
                    {
                        var retries = GetRetryCount(
                            delivery.BasicProperties.Headers);

                        var destination = retries < MaxRetries
                            ? OrderNotificationTopology.RetryQueue
                            : OrderNotificationTopology.FailedQueue;

                        try
                        {
                            var properties = new BasicProperties
                            {
                                Persistent = true,
                                ContentType = "application/json",
                                MessageId = delivery.BasicProperties.MessageId,
                                Headers = new Dictionary<string, object?>
                                {
                                    ["retry-count"] = retries + 1
                                }
                            };

                            await channel.BasicPublishAsync(
                                exchange: string.Empty,
                                routingKey: destination,
                                mandatory: true,
                                basicProperties: properties,
                                body: body,
                                cancellationToken: stoppingToken);

                            await channel.BasicAckAsync(
                                delivery.DeliveryTag, false, stoppingToken);

                            _logger.LogWarning(
                                exception,
                                "Notification sent to {Queue} after attempt {Attempt}.",
                                destination,
                                retries + 1);
                        }
                        catch (Exception publishException)
                        {
                            _logger.LogError(
                                publishException,
                                "Could not forward notification; requeuing original.");

                            await channel.BasicNackAsync(
                                delivery.DeliveryTag,
                                multiple: false,
                                requeue: true,
                                cancellationToken: stoppingToken);
                        }
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: OrderNotificationTopology.MainQueue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("Waiting for order notifications.");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "RabbitMQ connection failed; retrying in 5 seconds.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private static int GetRetryCount(
        IDictionary<string, object?>? headers)
    {
        if (headers is null ||
            !headers.TryGetValue("retry-count", out var value))
        {
            return 0;
        }

        return value switch
        {
            int number => number,
            long number => checked((int)number),
            byte number => number,
            _ => 0
        };
    }
}
