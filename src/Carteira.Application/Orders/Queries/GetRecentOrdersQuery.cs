using Carteira.Domain.Orders;

namespace Carteira.Application.Orders.Queries;

public sealed record GetRecentOrdersQuery(int Limit = 100);

public sealed class GetRecentOrdersQueryHandler
{
    private readonly IOrderQueryRepository _repository;

    public GetRecentOrdersQueryHandler(IOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Order>> HandleAsync(
        GetRecentOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Limit is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(query.Limit));

        return _repository.GetRecentAsync(
            query.Limit,
            cancellationToken);
    }
}
