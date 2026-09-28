using System.Text.RegularExpressions;

namespace Carteira.Domain.Orders;

public sealed class Order
{
    public Guid Id { get; private set; }
    public long Sequence { get; private set; }
    public string Symbol { get; private set; }
    public OrderSide Side { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Price { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Order(
        Guid id,
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal price,
        DateTimeOffset createdAt)
    {
        Id = id;
        Symbol = symbol;
        Side = side;
        Quantity = quantity;
        Price = price;
        CreatedAt = createdAt;
    }

    public static Order Create(
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal price,
        DateTimeOffset createdAt)
    {
        var normalizedSymbol = symbol?.Trim().ToUpperInvariant();

        if (normalizedSymbol is null ||
            !Regex.IsMatch(normalizedSymbol, "^[A-Z0-9]{3,12}$"))
            throw new ArgumentException("O ativo deve ter entre 3 e 12 letras ou números.", nameof(symbol));

        if (!Enum.IsDefined(side))
            throw new ArgumentException("O tipo da ordem é inválido.", nameof(side));

        if (quantity <= 0 || decimal.Round(quantity, 4) != quantity)
            throw new ArgumentException("A quantidade deve ser positiva e ter até 4 casas decimais.", nameof(quantity));

        if (price <= 0 || decimal.Round(price, 2) != price)
            throw new ArgumentException("O preço deve ser positivo e ter até 2 casas decimais.", nameof(price));

        return new Order(Guid.NewGuid(), normalizedSymbol, side, quantity, price, createdAt);
    }
}


