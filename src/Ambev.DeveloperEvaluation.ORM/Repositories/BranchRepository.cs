using Ambev.DeveloperEvaluation.Common.Tracing;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

// Work item: TASK-016 (FEAT-010)
/// <summary>
/// Implementation of IBranchRepository using Entity Framework Core
/// </summary>
public class BranchRepository : IBranchRepository
{
    private readonly DefaultContext _context;

    // Work item: TASK-025 (FEAT-011)
    private static readonly SortField[] DefaultOrder = [new("Name", false)];

    /// <summary>
    /// Initializes a new instance of BranchRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public BranchRepository(DefaultContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new branch in the database
    /// </summary>
    public async Task<Branch> CreateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        await _context.Branches.AddAsync(branch, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return branch;
    }

    /// <summary>
    /// Retrieves a tracked branch by its unique identifier
    /// </summary>
    public async Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    /// <summary>
    /// Saves the changes made to a tracked branch
    /// </summary>
    public async Task<Branch> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return branch;
    }

    /// <summary>
    /// Deletes a branch from the database
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var branch = await GetByIdAsync(id, cancellationToken);
        if (branch == null)
            return false;

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Work item: BUG-008 (FEAT-010), TASK-025 (FEAT-011), TASK-047 (FEAT-017)
    /// <summary>
    /// Retrieves one page of branches that match the query's filters, in the query's order or else by name. A page
    /// past the last one, including one whose offset does not fit an int, is empty
    /// </summary>
    public async Task<(IReadOnlyList<Branch> Items, int TotalCount)> ListAsync(ListQuery query, CancellationToken cancellationToken = default)
    {
        var rows = _context.Branches.AsNoTracking()
            .ApplyFilters(query.Filters)
            .ApplyOrder(query.Order, DefaultOrder);
        var totalCount = await rows.CountAsync(cancellationToken);
        var offset = (long)(query.Page - 1) * query.Size;
        StepTrace.Step("CMN-LST-09", "Filter, order with Id as tiebreak, count, then page",
            [("filters", query.Filters.Count), ("sortFields", query.Order.Count), ("total", totalCount), ("page", query.Page), ("size", query.Size),
             ("pastEnd", offset >= totalCount)]);
        if (offset >= totalCount)
            return ([], totalCount);

        var items = await rows.Skip((int)offset).Take(query.Size).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
