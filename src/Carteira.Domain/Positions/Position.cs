using Carteira.Domain.Orders;

namespace Carteira.Domain.Positions;

public sealed class Position
{
    public string Symbol { get; }
    public decimal Quantity { get; }
    public decimal AveragePrice { get; }

    private Position(string symbol, decimal quantity, decimal averagePrice)
    {
        Symbol = symbol;
        Quantity = quantity;
        AveragePrice = averagePrice;
    }

    public static Position FromFirstBuy(Order order)
    {
        if (order.Side != OrderSide.Buy)
            throw new InvalidOperationException(
                "Uma posição não pode começar com uma venda.");

        return new Position(order.Symbol, order.Quantity, order.Price);
    }

    public Position Apply(Order order)
    {
        if (order.Symbol != Symbol)
            throw new ArgumentException(
                "A ordem pertence a outro ativo.", nameof(order));

        if (order.Side == OrderSide.Buy)
        {
            var newQuantity = Quantity + order.Quantity;
            var newAveragePrice =
                ((Quantity * AveragePrice) + (order.Quantity * order.Price))
                / newQuantity;

            return new Position(Symbol, newQuantity, newAveragePrice);
        }

        if (order.Quantity > Quantity)
            throw new InvalidOperationException(
                "A venda excede a posição disponível.");

        var remainingQuantity = Quantity - order.Quantity;

        return new Position(
            Symbol,
            remainingQuantity,
            remainingQuantity == 0 ? 0 : AveragePrice);
    }
}
