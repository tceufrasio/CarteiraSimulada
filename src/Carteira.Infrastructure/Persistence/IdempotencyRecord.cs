namespace Carteira.Infrastructure.Persistence;

public sealed class IdempotencyRecord
{
    public Guid Key { get; private set; }
    public string Fingerprint { get; private set; }
    public Guid OrderId { get; private set; }

    private IdempotencyRecord(
        Guid key,
        string fingerprint,
        Guid orderId)
    {
        Key = key;
        Fingerprint = fingerprint;
        OrderId = orderId;
    }

    public static IdempotencyRecord Create(
        Guid key,
        string fingerprint,
        Guid orderId)
    {
        return new IdempotencyRecord(key, fingerprint, orderId);
    }
}
