using Carteira.Application.Orders.Queries;
using Carteira.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Carteira.Infrastructure.Persistence;

public sealed class EfOrderQueryRepository : IOrderQueryRepository
{
    private readonly CarteiraDbContext _dbContext;

    public EfOrderQueryRepository(CarteiraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                order => order.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
