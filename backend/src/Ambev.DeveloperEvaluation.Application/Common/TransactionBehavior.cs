using Ambev.DeveloperEvaluation.Domain.Repositories;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Common;

// Work item: TD-006
/// <summary>
/// Runs the handler of every <see cref="ITransactionalCommand"/> inside an explicit transaction.
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of TransactionBehavior
    /// </summary>
    /// <param name="unitOfWork">The unit of work that owns the transaction</param>
    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Handles the request, wrapping transactional commands in a transaction.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalCommand)
            return await next();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next();
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }
}
