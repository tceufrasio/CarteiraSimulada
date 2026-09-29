using Carteira.Application.Orders.Notifications;
using Carteira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Carteira.Infrastructure.Messaging;

public sealed class ProcessOrderNotification
{
    private readonly CarteiraDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ProcessOrderNotification(
        CarteiraDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HandleAsync(
        OrderRegisteredNotification notification,
        CancellationToken cancellationToken)
    {
        if (notification.OrderId == Guid.Empty ||
            string.IsNullOrWhiteSpace(notification.Symbol) ||
            notification.Symbol.Length > 12)
        {
            throw new ArgumentException("Invalid order notification.");
        }

        await _dbContext.ProcessedOrderNotifications.AddAsync(
            ProcessedOrderNotification.Create(
                notification.OrderId,
                notification.Symbol,
                _timeProvider.GetUtcNow()),
            cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "PK_processed_order_notifications"
            })
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }
}
