using GatewayService.AccountCharge.Application.Abstractions;
using MediatR;

namespace GatewayService.AccountCharge.Application.Behaviors;

/// <summary>
/// Wraps command handlers in a transaction boundary (if infra supports).
/// If your UoW is DbContext-based, ensure SaveChanges in handlers;
/// this behavior can be extended to begin/commit/rollback if needed.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IUnitOfWork _uow;

    public TransactionBehavior(IUnitOfWork uow) => _uow = uow;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // For simplicity, just call next. If you want to enforce SaveChanges here, 
        // you can check type of request and call _uow.SaveChangesAsync after next().
        var response = await next();
        return response;
    }
}
