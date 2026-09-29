using Carteira.Application.Positions.Queries;
using Carteira.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Carteira.Infrastructure.Persistence;

public sealed class EfPositionOrderReader : IPositionOrderReader
{
    private readonly CarteiraDbContext _dbContext;

    public EfPositionOrderReader(CarteiraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Order>> GetBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Symbol == symbol)
            .OrderBy(order => order.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .OrderBy(order => order.Sequence)
            .ToListAsync(cancellationToken);
    }
}
