using Carteira.Application.Orders.Commands;
using Carteira.Domain.Orders;

namespace Carteira.Tests;

public class RegisterOrderCommandTests
{
    [Fact]
    public async Task EquivalentRequestsProduceSameFingerprint()
    {
        var writer = new RecordingWriter();
        var handler = new RegisterOrderCommandHandler(
            writer, TimeProvider.System);
        var key = Guid.NewGuid();

        await handler.HandleAsync(new RegisterOrderCommand(
            key, " petr4 ", OrderSide.Buy, 2m, 35.5m));

        var firstFingerprint = writer.Fingerprint;

        await handler.HandleAsync(new RegisterOrderCommand(
            key, "PETR4", OrderSide.Buy, 2.0000m, 35.50m));

        Assert.Equal(firstFingerprint, writer.Fingerprint);
        Assert.Equal(key, writer.Key);
        Assert.Equal("PETR4", writer.Order?.Symbol);
    }

    [Fact]
    public async Task InvalidOrderDoesNotReachWriter()
    {
        var writer = new RecordingWriter();
        var handler = new RegisterOrderCommandHandler(
            writer, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new RegisterOrderCommand(
                Guid.NewGuid(), "AB", OrderSide.Buy, 2m, 35.50m)));

        Assert.Null(writer.Order);
    }

    [Fact]
    public async Task EmptyKeyDoesNotReachWriter()
    {
        var writer = new RecordingWriter();
        var handler = new RegisterOrderCommandHandler(
            writer, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new RegisterOrderCommand(
                Guid.Empty, "PETR4", OrderSide.Buy, 2m, 35.50m)));

        Assert.Null(writer.Order);
    }

    private sealed class RecordingWriter : IIdempotentOrderWriter
    {
        public Guid Key { get; private set; }
        public string? Fingerprint { get; private set; }
        public Order? Order { get; private set; }

        public Task<RegisterOrderResult> SaveAsync(
            Guid key,
            string fingerprint,
            Order order,
            CancellationToken cancellationToken)
        {
            Key = key;
            Fingerprint = fingerprint;
            Order = order;

            return Task.FromResult(new RegisterOrderResult(
                RegisterOrderStatus.Created, order));
        }
    }
}
