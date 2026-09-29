using Carteira.Application.Positions.Queries;
using Carteira.Domain.Orders;

namespace Carteira.Tests;

public class GetPortfolioSummaryQueryTests
{
    [Fact]
    public async Task HandleAsync_UsesCostOfOpenPositionsAndResultsOfAllSales()
    {
        var orders = new[]
        {
            Create("PETR4", OrderSide.Buy, 2m, 10m),
            Create("PETR4", OrderSide.Buy, 2m, 20m),
            Create("PETR4", OrderSide.Sell, 1m, 25m),

            Create("VALE3", OrderSide.Buy, 2m, 30m),
            Create("VALE3", OrderSide.Sell, 2m, 25m),

            Create("ITUB4", OrderSide.Buy, 1m, 40m),
            Create("ITUB4", OrderSide.Sell, 1m, 50m),
            Create("ITUB4", OrderSide.Buy, 1m, 45m)
        };

        var handler = new GetPortfolioSummaryQueryHandler(
            new InMemoryReader(orders));

        var result = await handler.HandleAsync(
            new GetPortfolioSummaryQuery());

        Assert.Equal(90m, result.InvestedAmount);
        Assert.Equal(10m, result.RealizedProfitLoss);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeroForEmptyPortfolio()
    {
        var handler = new GetPortfolioSummaryQueryHandler(
            new InMemoryReader(Array.Empty<Order>()));

        var result = await handler.HandleAsync(
            new GetPortfolioSummaryQuery());

        Assert.Equal(0m, result.InvestedAmount);
        Assert.Equal(0m, result.RealizedProfitLoss);
    }

    private static Order Create(
        string symbol, OrderSide side, decimal quantity, decimal price) =>
        Order.Create(symbol, side, quantity, price, DateTimeOffset.UtcNow);

    private sealed class InMemoryReader : IPositionOrderReader
    {
        private readonly IReadOnlyList<Order> _orders;

        public InMemoryReader(IReadOnlyList<Order> orders)
        {
            _orders = orders;
        }

        public Task<IReadOnlyList<Order>> GetAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(_orders);

        public Task<IReadOnlyList<Order>> GetBySymbolAsync(
            string symbol,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(
                _orders.Where(order => order.Symbol == symbol).ToArray());
    }
}
