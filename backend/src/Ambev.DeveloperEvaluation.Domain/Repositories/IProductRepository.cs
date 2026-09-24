using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// Repository interface for Product entity operations
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Creates a new product in the repository
    /// </summary>
    /// <param name="product">The product to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created product</returns>
    Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a product by its unique identifier
    /// </summary>
    /// <param name="id">The unique identifier of the product</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tracked product if found, null otherwise</returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Work item: FEAT-013
    /// <summary>
    /// Retrieves a product by its code, without tracking
    /// </summary>
    /// <param name="code">The product code, already trimmed and in upper case</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The product if found, null otherwise</returns>
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the products with the given identifiers in a single query
    /// </summary>
    /// <param name="ids">The product identifiers; duplicates are ignored</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The products found; unknown ids are absent from the result</returns>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the changes made to a product loaded through <see cref="GetByIdAsync"/>
    /// </summary>
    /// <param name="product">The tracked product</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated product</returns>
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product from the repository
    /// </summary>
    /// <param name="id">The unique identifier of the product to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the product was deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves one page of products ordered by description
    /// </summary>
    /// <param name="page">The page number, starting at 1</param>
    /// <param name="size">The page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The products on the page and the total number of products</returns>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(int page, int size, CancellationToken cancellationToken = default);
}
