using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Carteira.Application.Orders.Notifications;
using Carteira.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Carteira.IntegrationTests;

public class OrdersHttpDatabaseTests
{
    [Fact]
    public async Task RepeatedPost_ReturnsSameOrderWithoutCreatingAnother()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CARTEIRA_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Configure CARTEIRA_TEST_CONNECTION para carteira_tests.");

        if (new NpgsqlConnectionStringBuilder(connectionString).Database
            != "carteira_tests")
        {
            throw new InvalidOperationException(
                "O teste HTTP só pode usar carteira_tests.");
        }

        var key = Guid.NewGuid();
        var symbol = "H" + Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Carteira"] = connectionString
                        });
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IDbContextOptionsConfiguration<CarteiraDbContext>>();
                    services.RemoveAll<DbContextOptions<CarteiraDbContext>>();
                    services.AddDbContext<CarteiraDbContext>(options =>
                        options.UseNpgsql(connectionString));

                    services.AddSingleton<IOrderNotificationPublisher>(
                        new NoOpPublisher());
                });
            });

        try
        {
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var appDb = scope.ServiceProvider
                    .GetRequiredService<CarteiraDbContext>();

                if (appDb.Database.GetDbConnection().Database != "carteira_tests")
                {
                    throw new InvalidOperationException(
                        "A API de teste não está conectada a carteira_tests. POST cancelado.");
                }
            }

            async Task<HttpResponseMessage> SendAsync()
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post, "/api/orders");
                request.Headers.Add("Idempotency-Key", key.ToString("D"));
                request.Content = JsonContent.Create(new
                {
                    symbol,
                    side = "BUY",
                    quantity = 1,
                    price = 10
                });
                return await client.SendAsync(request);
            }

            using var first = await SendAsync();
            using var replay = await SendAsync();

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);

            using var firstJson = JsonDocument.Parse(
                await first.Content.ReadAsStringAsync());
            using var replayJson = JsonDocument.Parse(
                await replay.Content.ReadAsStringAsync());

            var firstId = firstJson.RootElement.GetProperty("id").GetGuid();
            var replayId = replayJson.RootElement.GetProperty("id").GetGuid();

            Assert.Equal(firstId, replayId);
            Assert.True(
                replayJson.RootElement.GetProperty("replayed").GetBoolean());

            var verifyOptions = new DbContextOptionsBuilder<CarteiraDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            await using var verifyDb = new CarteiraDbContext(verifyOptions);
            Assert.Equal(
                1,
                await verifyDb.Orders.CountAsync(order => order.Symbol == symbol));
        }
        finally
        {
            var options = new DbContextOptionsBuilder<CarteiraDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            await using var db = new CarteiraDbContext(options);
            await db.IdempotencyRecords
                .Where(record => record.Key == key)
                .ExecuteDeleteAsync();
            await db.Orders
                .Where(order => order.Symbol == symbol)
                .ExecuteDeleteAsync();
        }
    }

    private sealed class NoOpPublisher : IOrderNotificationPublisher
    {
        public Task PublishAsync(
            OrderRegisteredNotification notification,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
