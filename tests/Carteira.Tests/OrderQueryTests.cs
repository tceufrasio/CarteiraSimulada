using Carteira.Application.Orders.Queries;
using Carteira.Domain.Orders;

namespace Carteira.Tests;

public class OrderQueryTests
{
    [Fact]
    public async Task GetById_ReturnsExistingOrder()
    {
        var order = Order.Create(
            "PETR4", OrderSide.Buy, 2m, 35.50m, DateTimeOffset.UtcNow);
        var repository = new RecordingQueryRepository(order);
        var handler = new GetOrderByIdQueryHandler(repository);

        var result = await handler.HandleAsync(new GetOrderByIdQuery(order.Id));

        Assert.Same(order, result);
        Assert.Equal(order.Id, repository.RequestedId);
    }

    [Fact]
    public async Task GetById_ReturnsNullWhenOrderDoesNotExist()
    {
        var repository = new RecordingQueryRepository();
        var handler = new GetOrderByIdQueryHandler(repository);

        var result = await handler.HandleAsync(
            new GetOrderByIdQuery(Guid.NewGuid()));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecent_PassesLimitToRepository()
    {
        var order = Order.Create(
            "VALE3", OrderSide.Buy, 1m, 60m, DateTimeOffset.UtcNow);
        var repository = new RecordingQueryRepository(order);
        var handler = new GetRecentOrdersQueryHandler(repository);

        var result = await handler.HandleAsync(new GetRecentOrdersQuery(25));

        Assert.Single(result);
        Assert.Same(order, result[0]);
        Assert.Equal(25, repository.RequestedLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetRecent_RejectsLimitOutsideAllowedRange(int limit)
    {
        var repository = new RecordingQueryRepository();
        var handler = new GetRecentOrdersQueryHandler(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(new GetRecentOrdersQuery(limit)));

        Assert.Null(repository.RequestedLimit);
    }

    private sealed class RecordingQueryRepository : IOrderQueryRepository
    {
        private readonly Order? _order;

        public RecordingQueryRepository(Order? order = null)
        {
            _order = order;
        }

        public Guid? RequestedId { get; private set; }
        public int? RequestedLimit { get; private set; }

        public Task<Order?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            RequestedId = id;
            return Task.FromResult(_order?.Id == id ? _order : null);
        }

        public Task<IReadOnlyList<Order>> GetRecentAsync(
            int limit,
            CancellationToken cancellationToken)
        {
            RequestedLimit = limit;
            IReadOnlyList<Order> orders = _order is null
                ? Array.Empty<Order>()
                : new[] { _order };

            return Task.FromResult(orders);
        }
    }
}
