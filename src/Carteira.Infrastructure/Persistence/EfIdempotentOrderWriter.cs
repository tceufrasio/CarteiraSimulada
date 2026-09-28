using Carteira.Application.Orders.Commands;
using Carteira.Domain.Orders;
using Carteira.Domain.Positions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Carteira.Infrastructure.Persistence;

public sealed class EfIdempotentOrderWriter : IIdempotentOrderWriter
{
    private readonly CarteiraDbContext _dbContext;

    public EfIdempotentOrderWriter(CarteiraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegisterOrderResult> SaveAsync(
        Guid key,
        string fingerprint,
        Order order,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Serializes operations for the same symbol until commit or rollback.
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({order.Symbol}))",
                cancellationToken);

            // A replayed sale must be recognized before checking today's position.
            var previous = await _dbContext.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    record => record.Key == key,
                    cancellationToken);

            if (previous is not null)
                return await ResolvePreviousAsync(
                    previous, fingerprint, cancellationToken);

            var history = await _dbContext.Orders
                .AsNoTracking()
                .Where(existing => existing.Symbol == order.Symbol)
                .OrderBy(existing => existing.Sequence)
                .ToListAsync(cancellationToken);

            Position? position = null;

            foreach (var existing in history)
            {
                position = position is null
                    ? Position.FromFirstBuy(existing)
                    : position.Apply(existing);
            }

            if (order.Side == OrderSide.Sell && position is null)
                throw new InvalidOperationException(
                    "Não existe posição disponível para vender este ativo.");

            if (position is not null)
                position.Apply(order); // Validates available quantity.

            await _dbContext.Orders.AddAsync(order, cancellationToken);
            await _dbContext.IdempotencyRecords.AddAsync(
                IdempotencyRecord.Create(key, fingerprint, order.Id),
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RegisterOrderResult(
                RegisterOrderStatus.Created,
                order);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "PK_idempotency_records"
            })
        {
            // Handles simultaneous reuse of a key for different symbols.
            _dbContext.ChangeTracker.Clear();

            var previous = await _dbContext.IdempotencyRecords
                .AsNoTracking()
                .SingleAsync(record => record.Key == key, cancellationToken);

            return await ResolvePreviousAsync(
                previous, fingerprint, cancellationToken);
        }
    }

    private async Task<RegisterOrderResult> ResolvePreviousAsync(
        IdempotencyRecord previous,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (previous.Fingerprint != fingerprint)
        {
            return new RegisterOrderResult(
                RegisterOrderStatus.Conflict,
                null);
        }

        var existingOrder = await _dbContext.Orders
            .AsNoTracking()
            .SingleAsync(
                existing => existing.Id == previous.OrderId,
                cancellationToken);

        return new RegisterOrderResult(
            RegisterOrderStatus.Replayed,
            existingOrder);
    }
}
