using Carteira.Domain.Orders;

namespace Carteira.Application.Positions.Queries;

public interface IPositionOrderReader
{
    Task<IReadOnlyList<Order>> GetBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken);
}
