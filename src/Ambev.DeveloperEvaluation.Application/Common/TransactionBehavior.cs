using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Common.Tracing;
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

    // Work item: TASK-046 (FEAT-017)
    /// <summary>
    /// Handles the request, wrapping transactional commands in a transaction.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        StepTrace.Step("CMN-PIP-09", "TransactionBehavior, see CMN-TXN", [("request", typeof(TRequest).Name)]);
        var transactional = request is ITransactionalCommand;
        StepTrace.Step("CMN-TXN-01", "Command is an ITransactionalCommand?", [("request", typeof(TRequest).Name), ("transactional", transactional)]);
        if (!transactional)
        {
            StepTrace.Step("CMN-TXN-02", "Run the handler without a transaction", [("request", typeof(TRequest).Name)]);
            return await next();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        StepTrace.Step<TRequest>(SharedPoint.TransactionBegin, "CMN-TXN-03", "Begin the transaction", [("request", typeof(TRequest).Name)]);
        try
        {
            StepTrace.Step("CMN-TXN-04", "Run the handler", [("request", typeof(TRequest).Name)]);
            var response = await next();
            StepTrace.Step("CMN-TXN-05", "Handler threw?", [("threw", false)]);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            StepTrace.Step<TRequest>(SharedPoint.TransactionCommit, "CMN-TXN-06", "Commit",
                [("request", typeof(TRequest).Name), ("saleId", ((object?)response as SaleResult)?.Id ?? ((object?)request as DeleteSaleCommand)?.Id)]);
            return response;
        }
        catch (Exception exception)
        {
            StepTrace.Step("CMN-TXN-05", "Handler threw?", [("threw", true), ("exception", exception.GetType().Name)]);
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            StepTrace.Step<TRequest>(SharedPoint.TransactionRollback, "CMN-TXN-07", "Roll back and rethrow",
                [("request", typeof(TRequest).Name), ("exception", exception.GetType().Name)]);
            throw;
        }
    }
}
