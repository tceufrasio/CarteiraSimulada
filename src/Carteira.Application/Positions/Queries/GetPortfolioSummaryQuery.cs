using Carteira.Domain.Orders;
using Carteira.Domain.Positions;

namespace Carteira.Application.Positions.Queries;

public sealed record GetPortfolioSummaryQuery;

public sealed record PortfolioSummary(
    decimal InvestedAmount,
    decimal RealizedProfitLoss);

public sealed class GetPortfolioSummaryQueryHandler
{
    private readonly IPositionOrderReader _reader;

    public GetPortfolioSummaryQueryHandler(IPositionOrderReader reader)
    {
        _reader = reader;
    }

    public async Task<PortfolioSummary> HandleAsync(
        GetPortfolioSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var orders = await _reader.GetAllAsync(cancellationToken);
        var positions = new Dictionary<string, Position>();
        decimal realizedProfitLoss = 0;

        foreach (var order in orders)
        {
            if (!positions.TryGetValue(order.Symbol, out var current))
            {
                positions[order.Symbol] = Position.FromFirstBuy(order);
                continue;
            }

            if (order.Side == OrderSide.Sell)
            {
                realizedProfitLoss +=
                    (order.Price - current.AveragePrice) * order.Quantity;
            }

            positions[order.Symbol] = current.Apply(order);
        }

        var investedAmount = positions.Values
            .Where(position => position.Quantity > 0)
            .Sum(position => position.Quantity * position.AveragePrice);

        return new PortfolioSummary(investedAmount, realizedProfitLoss);
    }
}
