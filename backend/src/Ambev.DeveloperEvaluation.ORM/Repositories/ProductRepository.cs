using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// Implementation of IProductRepository using Entity Framework Core
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly DefaultContext _context;

    /// <summary>
    /// Initializes a new instance of ProductRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public ProductRepository(DefaultContext context)
    {
        _context = context;
    }

    // Work item: TASK-016 (FEAT-010), FEAT-013
    /// <summary>
    /// Creates a new product in the database. A code already stored raises <see cref="DuplicateEntryException"/>
    /// </summary>
    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
        await SaveChangesAsync(product, cancellationToken);
        return product;
    }

    /// <summary>
    /// Retrieves a tracked product by its unique identifier
    /// </summary>
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    // Work item: FEAT-013
    /// <summary>
    /// Retrieves a product by its code, without tracking
    /// </summary>
    public async Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    /// <summary>
    /// Retrieves the products with the given identifiers in a single query
    /// </summary>
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var distinctIds = ids.Distinct().ToList();
        return await _context.Products.AsNoTracking()
            .Where(p => distinctIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    // Work item: TASK-016 (FEAT-010), FEAT-013
    /// <summary>
    /// Saves the changes made to a tracked product. A code used by another product raises <see cref="DuplicateEntryException"/>
    /// </summary>
    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(product, cancellationToken);
        return product;
    }

    // Work item: FEAT-013
    /// <summary>
    /// Saves the pending changes, turning a violation of the unique code index into <see cref="DuplicateEntryException"/>,
    /// so a request that passed the handler's check concurrently with another gets the same answer
    /// </summary>
    private async Task SaveChangesAsync(Product product, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation,
                                               ConstraintName: "IX_Products_Code"
                                           })
        {
            throw new DuplicateEntryException($"Product with code {product.Code} already exists");
        }
    }

    /// <summary>
    /// Deletes a product from the database
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);
        if (product == null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Work item: BUG-008 (FEAT-010)
    /// <summary>
    /// Retrieves one page of products ordered by description. A page past the last one, including one whose offset does not fit
    /// an int, is empty
    /// </summary>
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().OrderBy(p => p.Description).ThenBy(p => p.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (long)(page - 1) * size;
        if (offset >= totalCount)
            return ([], totalCount);

        var items = await query.Skip((int)offset).Take(size).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
