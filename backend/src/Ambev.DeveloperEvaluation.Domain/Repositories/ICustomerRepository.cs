using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// Repository interface for Customer entity operations
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Creates a new customer in the repository
    /// </summary>
    /// <param name="customer">The customer to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created customer</returns>
    Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the customer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tracked customer if found, null otherwise</returns>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the changes made to a customer loaded through <see cref="GetByIdAsync"/>
    /// </summary>
    /// <param name="customer">The tracked customer</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated customer</returns>
    Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a customer from the repository
    /// </summary>
    /// <param name="id">The unique identifier of the customer to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the customer was deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves one page of customers ordered by name
    /// </summary>
    /// <param name="page">The page number, starting at 1</param>
    /// <param name="size">The page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The customers on the page and the total number of customers</returns>
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListAsync(int page, int size, CancellationToken cancellationToken = default);
}
