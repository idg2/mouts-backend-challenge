namespace Ambev.DeveloperEvaluation.Application.Common;

// Work item: TD-006
/// <summary>
/// Marks a command whose handler runs inside an explicit transaction opened by <see cref="TransactionBehavior{TRequest, TResponse}"/>.
/// </summary>
public interface ITransactionalCommand
{
}
