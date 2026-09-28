using Carteira.Domain.Orders;

namespace Carteira.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid Id);

public sealed class GetOrderByIdQueryHandler
{
    private readonly IOrderQueryRepository _repository;

    public GetOrderByIdQueryHandler(IOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public Task<Order?> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(query.Id, cancellationToken);
    }
}
