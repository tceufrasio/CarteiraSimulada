using Carteira.Application.Orders;
using Carteira.Application.Orders.Commands;
using Carteira.Application.Orders.Queries;
using Carteira.Application.Positions.Queries;
using Carteira.Domain.Orders;
using Carteira.Application.Orders.Notifications;
using Carteira.Infrastructure.Messaging;
using Carteira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Carteira")
    ?? throw new InvalidOperationException(
        "Configure a connection string 'Carteira'.");

builder.Services.AddDbContext<CarteiraDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddScoped<IIdempotentOrderWriter, EfIdempotentOrderWriter>();
builder.Services.AddScoped<RegisterOrderCommandHandler>();
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq");

builder.Services.AddSingleton<IOrderNotificationPublisher>(
    new RabbitMqOrderNotificationPublisher(rabbitMqConnection));
builder.Services.AddScoped<IOrderQueryRepository, EfOrderQueryRepository>();
builder.Services.AddScoped<GetOrderByIdQueryHandler>();
builder.Services.AddScoped<GetRecentOrdersQueryHandler>();
builder.Services.AddScoped<IPositionOrderReader, EfPositionOrderReader>();
builder.Services.AddScoped<GetPositionQueryHandler>();
builder.Services.AddScoped<GetOpenPositionsQueryHandler>();
builder.Services.AddScoped<GetPortfolioSummaryQueryHandler>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/orders", async (
    HttpRequest httpRequest,
    CreateOrderRequest request,
    RegisterOrderCommandHandler handler,
    IOrderNotificationPublisher publisher,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (!httpRequest.Headers.TryGetValue("Idempotency-Key", out var values) ||
        values.Count != 1 ||
        !Guid.TryParse(values[0], out var key) ||
        key == Guid.Empty)
    {
        return Results.BadRequest(new
        {
            error = "Envie um UUID válido no cabeçalho Idempotency-Key."
        });
    }

    if (!string.Equals(request.Side, "BUY", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(request.Side, "SELL", StringComparison.OrdinalIgnoreCase))
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["side"] = ["Use BUY ou SELL."]
            });
    }

    var side = Enum.Parse<OrderSide>(request.Side, true);

    try
    {
        var result = await handler.HandleAsync(
            new RegisterOrderCommand(
                key,
                request.Symbol,
                side,
                request.Quantity,
                request.Price),
            cancellationToken);

        if (result.Status == RegisterOrderStatus.Conflict)
        {
            return Results.Conflict(new
            {
                error = "Esta chave já foi usada com outro pedido."
            });
        }

        var order = result.Order!;

        if (result.Status == RegisterOrderStatus.Created)
        {
            try
            {
                using var timeout = new CancellationTokenSource(
                    TimeSpan.FromSeconds(5));

                await publisher.PublishAsync(
                    new OrderRegisteredNotification(
                        order.Id,
                        order.Symbol,
                        order.CreatedAt),
                    timeout.Token);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Order {OrderId} was saved, but its notification was not published.",
                    order.Id);
            }
        }

        var response = new
        {
            order.Id,
            order.Symbol,
            Side = order.Side.ToString().ToUpperInvariant(),
            order.Quantity,
            order.Price,
            order.CreatedAt,
            Replayed = result.Status == RegisterOrderStatus.Replayed
        };

        return result.Status == RegisterOrderStatus.Created
            ? Results.Created($"/api/orders/{order.Id}", response)
            : Results.Ok(response);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                [exception.ParamName ?? "order"] = [exception.Message]
            });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});
app.MapGet("/api/orders/{id:guid}", async (
    Guid id,
    GetOrderByIdQueryHandler getOrder,
    CancellationToken cancellationToken) =>
{
    var order = await getOrder.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);

    return order is null
        ? Results.NotFound()
        : Results.Ok(new
        {
            order.Id,
            order.Symbol,
            Side = order.Side.ToString().ToUpperInvariant(),
            order.Quantity,
            order.Price,
            order.CreatedAt
        });
});
app.MapGet("/api/orders", async (
    GetRecentOrdersQueryHandler getRecentOrders,
    CancellationToken cancellationToken) =>
{
    var orders = await getRecentOrders.HandleAsync(new GetRecentOrdersQuery(), cancellationToken);

    return Results.Ok(orders.Select(order => new
    {
        order.Id,
        order.Symbol,
        Side = order.Side.ToString().ToUpperInvariant(),
        order.Quantity,
        order.Price,
        order.CreatedAt
    }));
});
app.MapGet("/api/portfolio/summary", async (
    GetPortfolioSummaryQueryHandler handler,
    CancellationToken cancellationToken) =>
{
    var summary = await handler.HandleAsync(
        new GetPortfolioSummaryQuery(),
        cancellationToken);

    return Results.Ok(summary);
});
app.MapGet("/api/positions", async (
    GetOpenPositionsQueryHandler handler,
    CancellationToken cancellationToken) =>
{
    var positions = await handler.HandleAsync(
        new GetOpenPositionsQuery(),
        cancellationToken);

    return Results.Ok(positions.Select(position => new
    {
        position.Symbol,
        position.Quantity,
        position.AveragePrice
    }));
});
app.MapGet("/api/positions/{symbol}", async (
    string symbol,
    GetPositionQueryHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var position = await handler.HandleAsync(
            new GetPositionQuery(symbol),
            cancellationToken);

        return position is null
            ? Results.NotFound()
            : Results.Ok(new
            {
                position.Symbol,
                position.Quantity,
                position.AveragePrice
            });
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["symbol"] = [exception.Message]
            });
    }
});
app.Run();

record CreateOrderRequest(string Symbol, string Side, decimal Quantity, decimal Price);

public partial class Program { }
