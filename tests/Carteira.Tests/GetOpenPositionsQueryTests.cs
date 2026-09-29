using Carteira.Application.Positions.Queries;
using Carteira.Domain.Orders;

namespace Carteira.Tests;

public class GetOpenPositionsQueryTests
{
    [Fact]
    public async Task HandleAsync_ReturnsOpenPositionsAndSkipsClosedOnes()
    {
        var orders = new[]
        {
            Create("PETR4", OrderSide.Buy, 2m, 10m),
            Create("VALE3", OrderSide.Buy, 3m, 20m),
            Create("PETR4", OrderSide.Buy, 2m, 20m),
            Create("VALE3", OrderSide.Sell, 3m, 25m),
            Create("ITUB4", OrderSide.Buy, 1m, 30m)
        };

        var handler = new GetOpenPositionsQueryHandler(
            new InMemoryReader(orders));

        var result = await handler.HandleAsync(
            new GetOpenPositionsQuery());

        Assert.Equal(2, result.Count);
        Assert.Equal("ITUB4", result[0].Symbol);
        Assert.Equal(1m, result[0].Quantity);
        Assert.Equal("PETR4", result[1].Symbol);
        Assert.Equal(4m, result[1].Quantity);
        Assert.Equal(15m, result[1].AveragePrice);
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
