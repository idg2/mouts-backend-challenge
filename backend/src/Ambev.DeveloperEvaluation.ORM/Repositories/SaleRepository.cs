using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// Implementation of ISaleRepository using Entity Framework Core. Every write is a single SaveChangesAsync,
/// which EF Core runs in one transaction, so a sale and its items are stored together or not at all.
/// </summary>
public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    // Work item: TASK-025 (FEAT-011)
    private static readonly SortField[] DefaultOrder = [new("SaleNumber", false)];

    /// <summary>
    /// Initializes a new instance of SaleRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new sale with its items in the database
    /// </summary>
    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    // Work item: TD-010 (FEAT-010)
    /// <summary>
    /// Retrieves a tracked sale with its items, ordered by line number, by its unique identifier
    /// </summary>
    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(s => s.Items.OrderBy(i => i.LineNumber))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>
    /// Saves the changes made to a tracked sale and its items
    /// </summary>
    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    /// <summary>
    /// Deletes a sale and its items from the database
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await GetByIdAsync(id, cancellationToken);
        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Work item: BUG-008 (FEAT-010), TASK-025 (FEAT-011)
    /// <summary>
    /// Retrieves one page of sales, without their items, that match the query's filters, in the query's order or else by sale number. A page
    /// past the last one, including one whose offset does not fit an int, is empty
    /// </summary>
    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(ListQuery query, CancellationToken cancellationToken = default)
    {
        var rows = _context.Sales.AsNoTracking()
            .ApplyFilters(query.Filters)
            .ApplyOrder(query.Order, DefaultOrder);
        var totalCount = await rows.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.Size;
        if (offset >= totalCount)
            return ([], totalCount);

        var items = await rows.Skip((int)offset).Take(query.Size).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
