using Carteira.Application.Orders.Commands;
using Carteira.Domain.Orders;
using Carteira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Carteira.IntegrationTests;

public class IdempotentOrderWriterTests
{
    [Fact]
    public async Task SameKeyCreatesOnceThenReplaysAndRejectsDifferentRequest()
    {
        var key = Guid.NewGuid();
        var symbol = "T" + Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        Guid? createdOrderId = null;

        try
        {
            var order = Order.Create(
                symbol, OrderSide.Buy, 2m, 10m, DateTimeOffset.UtcNow);

            await using (var db = CreateDbContext())
            {
                var writer = new EfIdempotentOrderWriter(db);
                var first = await writer.SaveAsync(
                    key, "pedido-original", order, CancellationToken.None);

                Assert.Equal(RegisterOrderStatus.Created, first.Status);
                Assert.NotNull(first.Order);
                createdOrderId = first.Order.Id;
            }

            await using (var db = CreateDbContext())
            {
                var writer = new EfIdempotentOrderWriter(db);
                var repeatedOrder = Order.Create(
                    symbol, OrderSide.Buy, 2m, 10m, DateTimeOffset.UtcNow);

                var replay = await writer.SaveAsync(
                    key, "pedido-original", repeatedOrder, CancellationToken.None);

                Assert.Equal(RegisterOrderStatus.Replayed, replay.Status);
                Assert.Equal(createdOrderId, replay.Order?.Id);
            }

            await using (var db = CreateDbContext())
            {
                var writer = new EfIdempotentOrderWriter(db);
                var differentOrder = Order.Create(
                    symbol, OrderSide.Buy, 3m, 10m, DateTimeOffset.UtcNow);

                var conflict = await writer.SaveAsync(
                    key, "pedido-diferente", differentOrder, CancellationToken.None);

                Assert.Equal(RegisterOrderStatus.Conflict, conflict.Status);
            }

            await using (var db = CreateDbContext())
            {
                Assert.Equal(1, await db.IdempotencyRecords.CountAsync(
                    record => record.Key == key));
                Assert.Equal(1, await db.Orders.CountAsync(
                    existing => existing.Symbol == symbol));
            }
        }
        finally
        {
            await using var db = CreateDbContext();

            await db.IdempotencyRecords
                .Where(record => record.Key == key)
                .ExecuteDeleteAsync();

            if (createdOrderId.HasValue)
            {
                await db.Orders
                    .Where(order => order.Id == createdOrderId.Value)
                    .ExecuteDeleteAsync();
            }
        }
    }

    [Fact]
    public async Task ConcurrentSales_AllowOnlyOneWhenOneUnitIsAvailable()
    {
        var symbol = "C" + Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        var buyKey = Guid.NewGuid();
        var firstSellKey = Guid.NewGuid();
        var secondSellKey = Guid.NewGuid();
        var keys = new[] { buyKey, firstSellKey, secondSellKey };

        try
        {
            await using (var db = CreateDbContext())
            {
                var writer = new EfIdempotentOrderWriter(db);
                var buy = Order.Create(
                    symbol, OrderSide.Buy, 1m, 10m, DateTimeOffset.UtcNow);

                var result = await writer.SaveAsync(
                    buyKey, "compra", buy, CancellationToken.None);

                Assert.Equal(RegisterOrderStatus.Created, result.Status);
            }

            async Task<string> SellAsync(Guid key)
            {
                await using var db = CreateDbContext();
                var writer = new EfIdempotentOrderWriter(db);
                var sell = Order.Create(
                    symbol, OrderSide.Sell, 1m, 12m, DateTimeOffset.UtcNow);

                try
                {
                    var result = await writer.SaveAsync(
                        key, $"venda-{key}", sell, CancellationToken.None);

                    return result.Status == RegisterOrderStatus.Created
                        ? "created"
                        : "unexpected";
                }
                catch (InvalidOperationException)
                {
                    return "rejected";
                }
            }

            var results = await Task.WhenAll(
                SellAsync(firstSellKey),
                SellAsync(secondSellKey));

            Assert.Equal(1, results.Count(result => result == "created"));
            Assert.Equal(1, results.Count(result => result == "rejected"));

            await using (var db = CreateDbContext())
            {
                var history = await db.Orders.AsNoTracking()
                    .Where(order => order.Symbol == symbol)
                    .ToListAsync();

                Assert.Equal(2, history.Count);
                Assert.Equal(1, history.Count(
                    order => order.Side == OrderSide.Sell));
            }
        }
        finally
        {
            await using var db = CreateDbContext();

            await db.IdempotencyRecords
                .Where(record => keys.Contains(record.Key))
                .ExecuteDeleteAsync();

            await db.Orders
                .Where(order => order.Symbol == symbol)
                .ExecuteDeleteAsync();
        }
    }
    private static CarteiraDbContext CreateDbContext()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CARTEIRA_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Configure CARTEIRA_TEST_CONNECTION para o banco carteira_tests.");

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
