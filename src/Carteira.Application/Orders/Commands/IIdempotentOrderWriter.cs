using Carteira.Domain.Orders;

namespace Carteira.Application.Orders.Commands;

public enum RegisterOrderStatus
{
    Created,
    Replayed,
    Conflict
}

public sealed record RegisterOrderResult(
    RegisterOrderStatus Status,
    Order? Order);

public interface IIdempotentOrderWriter
{
    Task<RegisterOrderResult> SaveAsync(
        Guid key,
        string fingerprint,
        Order order,
        CancellationToken cancellationToken);
}
