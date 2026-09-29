namespace Carteira.Infrastructure.Persistence;

public sealed class ProcessedOrderNotification
{
    public Guid OrderId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; private set; }

    private ProcessedOrderNotification()
    {
    }

    public static ProcessedOrderNotification Create(
        Guid orderId,
        string symbol,
        DateTimeOffset processedAt)
    {
        return new ProcessedOrderNotification
        {
            OrderId = orderId,
            Symbol = symbol,
            ProcessedAt = processedAt
        };
    }
}
