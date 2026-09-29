using Carteira.Application.Orders.Notifications;
using Carteira.Infrastructure.Messaging;
using Carteira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Carteira.IntegrationTests;

public class ProcessOrderNotificationTests
{
    [Fact]
    public async Task SameOrderId_IsProcessedOnlyOnce()
    {
        var orderId = Guid.NewGuid();
        var notification = new OrderRegisteredNotification(
            orderId,
            "PETR4",
            DateTimeOffset.UtcNow);

        try
        {
            await using (var db = CreateDbContext())
            {
                var processor = new ProcessOrderNotification(db, TimeProvider.System);
                Assert.True(await processor.HandleAsync(
                    notification, CancellationToken.None));
            }

            await using (var db = CreateDbContext())
            {
                var processor = new ProcessOrderNotification(db, TimeProvider.System);
                Assert.False(await processor.HandleAsync(
                    notification, CancellationToken.None));
            }

            await using (var db = CreateDbContext())
            {
                Assert.Equal(1, await db.ProcessedOrderNotifications
                    .CountAsync(record => record.OrderId == orderId));
            }
        }
        finally
        {
            await using var db = CreateDbContext();
            await db.ProcessedOrderNotifications
                .Where(record => record.OrderId == orderId)
                .ExecuteDeleteAsync();
        }
    }

    private static CarteiraDbContext CreateDbContext()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CARTEIRA_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Configure CARTEIRA_TEST_CONNECTION para carteira_tests.");

        if (new NpgsqlConnectionStringBuilder(connectionString).Database
            != "carteira_tests")
        {
            throw new InvalidOperationException(
                "Os testes de integração só podem usar carteira_tests.");
        }

        var options = new DbContextOptionsBuilder<CarteiraDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CarteiraDbContext(options);
    }
}
