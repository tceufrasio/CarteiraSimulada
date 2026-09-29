using Carteira.Application.Orders.Queries;
using Microsoft.EntityFrameworkCore;

namespace Carteira.Infrastructure.Persistence;

public sealed class EfOrderSearchRepository : IOrderSearchRepository
{
    private readonly CarteiraDbContext _dbContext;

    public EfOrderSearchRepository(CarteiraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderPage> SearchAsync(
        SearchOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var orders = _dbContext.Orders.AsNoTracking();

        if (!string.IsNullOrEmpty(query.Symbol))
            orders = orders.Where(order => order.Symbol.Contains(query.Symbol));

        if (query.Side is { } side)
            orders = orders.Where(order => order.Side == side);

        var totalCount = await orders.CountAsync(cancellationToken);
        var items = await orders
            .OrderByDescending(order => order.Sequence)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new OrderPage(items, totalCount, query.Page, query.PageSize);
    }
}
