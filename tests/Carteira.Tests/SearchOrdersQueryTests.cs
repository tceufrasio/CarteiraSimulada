using Carteira.Application.Orders.Queries;
using Carteira.Domain.Orders;

namespace Carteira.Tests;

public class SearchOrdersQueryTests
{
    [Fact]
    public async Task NormalizesSymbolAndPassesPaginationAndSide()
    {
        var repository = new RecordingSearchRepository();
        var handler = new SearchOrdersQueryHandler(repository);

        var result = await handler.HandleAsync(
            new SearchOrdersQuery(2, 10, " petr4 ", OrderSide.Sell));

        Assert.Equal(2, repository.LastQuery?.Page);
        Assert.Equal(10, repository.LastQuery?.PageSize);
        Assert.Equal("PETR4", repository.LastQuery?.Symbol);
        Assert.Equal(OrderSide.Sell, repository.LastQuery?.Side);
        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task RejectsInvalidPagination(int page, int pageSize)
    {
        var repository = new RecordingSearchRepository();
        var handler = new SearchOrdersQueryHandler(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(new SearchOrdersQuery(page, pageSize)));

        Assert.Null(repository.LastQuery);
    }

    private sealed class RecordingSearchRepository : IOrderSearchRepository
    {
        public SearchOrdersQuery? LastQuery { get; private set; }

        public Task<OrderPage> SearchAsync(
            SearchOrdersQuery query,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(new OrderPage(Array.Empty<Order>(), 0,
                query.Page, query.PageSize));
        }
    }
}
