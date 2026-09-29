namespace Carteira.Application.Orders.Notifications;

public sealed record OrderRegisteredNotification(
    Guid OrderId,
    string Symbol,
    DateTimeOffset OccurredAt);

public interface IOrderNotificationPublisher
{
    Task PublishAsync(
        OrderRegisteredNotification notification,
        CancellationToken cancellationToken);
}
