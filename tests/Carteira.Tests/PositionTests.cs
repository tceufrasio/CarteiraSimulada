using Carteira.Domain.Orders;
using Carteira.Domain.Positions;

namespace Carteira.Tests;

public class PositionTests
{
    [Fact]
    public void TwoBuys_UpdateQuantityAndWeightedAveragePrice()
    {
        var position = Position.FromFirstBuy(
            CreateOrder("PETR4", OrderSide.Buy, 2m, 10m));

        position = position.Apply(
            CreateOrder("PETR4", OrderSide.Buy, 3m, 20m));

        Assert.Equal(5m, position.Quantity);
        Assert.Equal(16m, position.AveragePrice);
    }

    [Fact]
    public void PartialSell_KeepsAveragePrice()
    {
        var position = Position.FromFirstBuy(
            CreateOrder("PETR4", OrderSide.Buy, 5m, 16m));

        position = position.Apply(
            CreateOrder("PETR4", OrderSide.Sell, 2m, 25m));

        Assert.Equal(3m, position.Quantity);
        Assert.Equal(16m, position.AveragePrice);
    }

    [Fact]
    public void FullSell_ResetsPosition()
    {
        var position = Position.FromFirstBuy(
            CreateOrder("PETR4", OrderSide.Buy, 5m, 16m));

        position = position.Apply(
            CreateOrder("PETR4", OrderSide.Sell, 5m, 25m));

        Assert.Equal(0m, position.Quantity);
        Assert.Equal(0m, position.AveragePrice);
    }

    [Fact]
    public void SellWithoutPosition_IsRejected()
    {
        var sell = CreateOrder("PETR4", OrderSide.Sell, 1m, 25m);

        Assert.Throws<InvalidOperationException>(() =>
            Position.FromFirstBuy(sell));
    }

    [Fact]
    public void SellMoreThanAvailable_IsRejected()
    {
        var position = Position.FromFirstBuy(
            CreateOrder("PETR4", OrderSide.Buy, 2m, 10m));

        Assert.Throws<InvalidOperationException>(() =>
            position.Apply(CreateOrder("PETR4", OrderSide.Sell, 3m, 25m)));
    }

    [Fact]
    public void OrderForAnotherSymbol_IsRejected()
    {
        var position = Position.FromFirstBuy(
            CreateOrder("PETR4", OrderSide.Buy, 2m, 10m));

        Assert.Throws<ArgumentException>(() =>
            position.Apply(CreateOrder("VALE3", OrderSide.Buy, 1m, 20m)));
    }

    private static Order CreateOrder(
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal price)
    {
        return Order.Create(
            symbol, side, quantity, price, DateTimeOffset.UtcNow);
    }
}
