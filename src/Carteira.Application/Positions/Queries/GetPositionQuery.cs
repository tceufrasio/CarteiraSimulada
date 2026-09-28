using Carteira.Domain.Orders;
using Carteira.Domain.Positions;

namespace Carteira.Application.Positions.Queries;

public sealed record GetPositionQuery(string Symbol);

public sealed class GetPositionQueryHandler
{
    private readonly IPositionOrderReader _reader;

    public GetPositionQueryHandler(IPositionOrderReader reader)
    {
        _reader = reader;
    }

    public async Task<Position?> HandleAsync(
        GetPositionQuery query,
        CancellationToken cancellationToken = default)
    {
        var symbol = query.Symbol?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException(
                "Informe o ativo.", nameof(query.Symbol));

        var orders = await _reader.GetBySymbolAsync(
            symbol, cancellationToken);

        Position? position = null;

        foreach (var order in orders)
        {
            position = position is null
                ? Position.FromFirstBuy(order)
                : position.Apply(order);
        }

        return position;
    }
}
