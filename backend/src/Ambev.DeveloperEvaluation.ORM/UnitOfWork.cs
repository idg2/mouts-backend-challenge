using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.ORM;

// Work item: TD-006
/// <summary>
/// Implementation of IUnitOfWork over the scoped <see cref="DefaultContext"/>, so the repositories' SaveChangesAsync
/// calls join the open transaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DefaultContext _context;

    /// <summary>
    /// Initializes a new instance of UnitOfWork
    /// </summary>
    /// <param name="context">The database context</param>
    public UnitOfWork(DefaultContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        await _context.Database.BeginTransactionAsync(cancellationToken);

    /// <inheritdoc />
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) =>
        _context.Database.CommitTransactionAsync(cancellationToken);

    /// <inheritdoc />
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) =>
        _context.Database.RollbackTransactionAsync(cancellationToken);
}
