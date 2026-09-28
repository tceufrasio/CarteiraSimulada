using Carteira.Domain.Orders;

namespace Carteira.Tests;

public class OrderTests
{
    [Fact]
    public void Create_NormalizesSymbolAndKeepsValues()
    {
        var createdAt = DateTimeOffset.UtcNow;

        var order = Order.Create(" petr4 ", OrderSide.Buy, 2.5m, 35.50m, createdAt);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("PETR4", order.Symbol);
        Assert.Equal(OrderSide.Buy, order.Side);
        Assert.Equal(2.5m, order.Quantity);
        Assert.Equal(35.50m, order.Price);
        Assert.Equal(createdAt, order.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("ATIVO COM ESPAÇO")]
    public void Create_RejectsInvalidSymbol(string symbol)
    {
        Assert.Throws<ArgumentException>(() =>
            Order.Create(symbol, OrderSide.Buy, 1m, 10m, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_RejectsInvalidSide()
    {
        Assert.Throws<ArgumentException>(() =>
            Order.Create("PETR4", (OrderSide)99, 1m, 10m, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.00001)]
    public void Create_RejectsInvalidQuantity(decimal quantity)
    {
        Assert.Throws<ArgumentException>(() =>
            Order.Create("PETR4", OrderSide.Buy, quantity, 10m, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.001)]
    public void Create_RejectsInvalidPrice(decimal price)
    {
        Assert.Throws<ArgumentException>(() =>
            Order.Create("PETR4", OrderSide.Buy, 1m, price, DateTimeOffset.UtcNow));
    }
}
