using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Carteira.IntegrationTests;

public class OrdersHttpTests
{
    [Fact]
    public async Task PostWithoutIdempotencyKey_ReturnsBadRequest()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Carteira"] =
                                "Host=localhost;Database=unused;Username=unused;Password=unused"
                        });
                });
            });

        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                symbol = "PETR4",
                side = "BUY",
                quantity = 1,
                price = 10
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idempotency-Key", body);
    }
}
