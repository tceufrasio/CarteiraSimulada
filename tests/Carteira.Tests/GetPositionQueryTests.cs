using Carteira.Application.Positions.Queries;
using Carteira.Domain.Orders;

namespace Carteira.Tests;

public class GetPositionQueryTests
{
    [Fact]
    public async Task HandleAsync_RebuildsPositionFromOrders()
    {
        var orders = new[]
        {
            CreateOrder("PETR4", OrderSide.Buy, 2m, 10m),
            CreateOrder("PETR4", OrderSide.Buy, 3m, 20m),
            CreateOrder("PETR4", OrderSide.Sell, 1m, 25m)
        };
        var reader = new RecordingPositionOrderReader(orders);
        var handler = new GetPositionQueryHandler(reader);

        var position = await handler.HandleAsync(
            new GetPositionQuery(" petr4 "));

        Assert.NotNull(position);
        Assert.Equal("PETR4", reader.RequestedSymbol);
        Assert.Equal(4m, position.Quantity);
        Assert.Equal(16m, position.AveragePrice);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNullWhenThereAreNoOrders()
    {
        var reader = new RecordingPositionOrderReader();
        var handler = new GetPositionQueryHandler(reader);

        var position = await handler.HandleAsync(
            new GetPositionQuery("VALE3"));

        Assert.Null(position);
    }

    [Fact]
    public async Task HandleAsync_RejectsEmptySymbolBeforeReading()
    {
        var reader = new RecordingPositionOrderReader();
        var handler = new GetPositionQueryHandler(reader);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new GetPositionQuery(" ")));

        Assert.Null(reader.RequestedSymbol);
    }

    private static Order CreateOrder(
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal price)
    {
        return Order.Create(
            symbol, side, quantity, price, DateTimeOffset.UtcNow);
    }

    private sealed class RecordingPositionOrderReader : IPositionOrderReader
    {
        private readonly IReadOnlyList<Order> _orders;

        public RecordingPositionOrderReader(
            IReadOnlyList<Order>? orders = null)
        {
            _orders = orders ?? Array.Empty<Order>();
        }

        public string? RequestedSymbol { get; private set; }

        public Task<IReadOnlyList<Order>> GetBySymbolAsync(
            string symbol,
            CancellationToken cancellationToken)
        {
            RequestedSymbol = symbol;
            return Task.FromResult(_orders);
        }
    }
}
