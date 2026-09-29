using Carteira.Domain.Positions;

namespace Carteira.Application.Positions.Queries;

public sealed record GetOpenPositionsQuery;

public sealed class GetOpenPositionsQueryHandler
{
    private readonly IPositionOrderReader _reader;

    public GetOpenPositionsQueryHandler(IPositionOrderReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<Position>> HandleAsync(
        GetOpenPositionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var orders = await _reader.GetAllAsync(cancellationToken);
        var positions = new Dictionary<string, Position>();

        foreach (var order in orders)
        {
            positions[order.Symbol] = positions.TryGetValue(
                order.Symbol, out var current)
                ? current.Apply(order)
                : Position.FromFirstBuy(order);
        }

        return positions.Values
            .Where(position => position.Quantity > 0)
            .OrderBy(position => position.Symbol)
            .ToArray();
    }
}
