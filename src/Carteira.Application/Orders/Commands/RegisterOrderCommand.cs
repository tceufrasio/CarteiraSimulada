using System.Security.Cryptography;
using System.Text;
using Carteira.Domain.Orders;

namespace Carteira.Application.Orders.Commands;

public sealed record RegisterOrderCommand(
    Guid IdempotencyKey,
    string Symbol,
    OrderSide Side,
    decimal Quantity,
    decimal Price);

public sealed class RegisterOrderCommandHandler
{
    private readonly IIdempotentOrderWriter _writer;
    private readonly TimeProvider _timeProvider;

    public RegisterOrderCommandHandler(
        IIdempotentOrderWriter writer,
        TimeProvider timeProvider)
    {
        _writer = writer;
        _timeProvider = timeProvider;
    }

    public Task<RegisterOrderResult> HandleAsync(
        RegisterOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.IdempotencyKey == Guid.Empty)
            throw new ArgumentException(
                "A chave de idempotência não pode ser vazia.",
                nameof(command.IdempotencyKey));

        var order = Order.Create(
            command.Symbol,
            command.Side,
            command.Quantity,
            command.Price,
            _timeProvider.GetUtcNow());

        var normalizedRequest = FormattableString.Invariant(
            $"{order.Symbol}|{(int)order.Side}|{order.Quantity:F4}|{order.Price:F2}");

        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRequest)));

        return _writer.SaveAsync(
            command.IdempotencyKey,
            fingerprint,
            order,
            cancellationToken);
    }
}
