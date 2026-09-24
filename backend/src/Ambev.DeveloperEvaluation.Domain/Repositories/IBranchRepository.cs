using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// Repository interface for Branch entity operations
/// </summary>
public interface IBranchRepository
{
    /// <summary>
    /// Creates a new branch in the repository
    /// </summary>
    /// <param name="branch">The branch to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created branch</returns>
    Task<Branch> CreateAsync(Branch branch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a branch by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the branch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tracked branch if found, null otherwise</returns>
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the changes made to a branch loaded through <see cref="GetByIdAsync"/>
    /// </summary>
    /// <param name="branch">The tracked branch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated branch</returns>
    Task<Branch> UpdateAsync(Branch branch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a branch from the repository
    /// </summary>
    /// <param name="id">The unique identifier of the branch to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the branch was deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves one page of branches ordered by name
    /// </summary>
    /// <param name="page">The page number, starting at 1</param>
    /// <param name="size">The page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The branches on the page and the total number of branches</returns>
    Task<(IReadOnlyList<Branch> Items, int TotalCount)> ListAsync(int page, int size, CancellationToken cancellationToken = default);
}
