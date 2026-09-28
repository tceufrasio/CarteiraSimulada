using Carteira.Domain.Orders;

namespace Carteira.Application.Orders.Queries;

public interface IOrderQueryRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken);
}
