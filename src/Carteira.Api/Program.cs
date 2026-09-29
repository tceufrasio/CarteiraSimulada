using Carteira.Application.Orders;
using Carteira.Application.Orders.Commands;
using Carteira.Application.Orders.Queries;
using Carteira.Application.Positions.Queries;
using Carteira.Domain.Orders;
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
builder.Services.AddScoped<IOrderQueryRepository, EfOrderQueryRepository>();
builder.Services.AddScoped<GetOrderByIdQueryHandler>();
builder.Services.AddScoped<GetRecentOrdersQueryHandler>();
builder.Services.AddScoped<IPositionOrderReader, EfPositionOrderReader>();
builder.Services.AddScoped<GetPositionQueryHandler>();
builder.Services.AddScoped<GetOpenPositionsQueryHandler>();
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
