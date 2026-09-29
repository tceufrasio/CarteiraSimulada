using Carteira.Domain.Orders;

namespace Carteira.Application.Orders.Queries;

public sealed record SearchOrdersQuery(
    int Page = 1,
    int PageSize = 10,
    string? Symbol = null,
    OrderSide? Side = null);

public sealed record OrderPage(
    IReadOnlyList<Order> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IOrderSearchRepository
{
    Task<OrderPage> SearchAsync(
        SearchOrdersQuery query,
        CancellationToken cancellationToken);
}

public sealed class SearchOrdersQueryHandler
{
    private readonly IOrderSearchRepository _repository;

    public SearchOrdersQueryHandler(IOrderSearchRepository repository)
    {
        _repository = repository;
    }

    public Task<OrderPage> HandleAsync(
        SearchOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page is < 1 or > 100000)
            throw new ArgumentOutOfRangeException(nameof(query.Page));

        if (query.PageSize is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(query.PageSize));

        var symbol = query.Symbol?.Trim().ToUpperInvariant();
        if (symbol?.Length > 12)
            throw new ArgumentException("O filtro de ativo deve ter até 12 caracteres.", nameof(query));

        return _repository.SearchAsync(
            query with { Symbol = symbol }, cancellationToken);
    }
}
